using AzureServices;
using EntityDto;
using Microsoft.Extensions.Logging;
using RepositoryContract.Orders;

namespace AzureTableRepository.Orders
{
    public class OrdersRepository : IOrdersRepository
    {
        static readonly string syncName = $"sync_control/LastSyncDate_${typeof(ComandaVanzareEntry).Name}";
        static BlobAccessStorageService blobAccessStorageService = new();
        TableStorageService tableStorageService;
        public OrdersRepository()
        {
            tableStorageService = new TableStorageService();
        }
        public async Task<List<ComandaVanzareEntry>> GetOrders(string? table = null)
        {
            table = table ?? typeof(ComandaVanzareEntry).Name;
            return (await CacheManager.GetAll((from) => tableStorageService.Query<ComandaVanzareEntry>(t => t.Timestamp > from).ToList(), table)).ToList();
        }

        public async Task<List<ComandaVanzareEntry>> GetOrders(Func<ComandaVanzareEntry, bool> expr, string? table = null)
        {
            table = table ?? typeof(ComandaVanzareEntry).Name;
            return (await CacheManager.GetAll((from) => tableStorageService.Query<ComandaVanzareEntry>(t => expr(t) && t.Timestamp > from).ToList(), table)).ToList();
        }

        public async Task ImportOrders(IList<ComandaVanzare> items, DateTime when)
        {
            if (items.Count == 0) return;
            var newEntries = items.Select(ComandaVanzareEntry.create).GroupBy(ComandaVanzareEntry.PKey).ToDictionary(t => t.Key, MergeByHash);

            foreach (var item in newEntries)
            {
                var oldEntries = tableStorageService.Query<ComandaVanzareEntry>(t => t.PartitionKey == item.Key).ToList();
                var comparer = ComandaVanzareEntry.GetEqualityComparer();
                var comparerQuantity = ComandaVanzareEntry.GetEqualityComparer(true);

                var currentEntries = newEntries[item.Key];

                var exceptAdd = currentEntries.Except(oldEntries, comparer).ToList();
                var exceptDelete = oldEntries.Except(currentEntries, comparer).ToList();

                var intersectOld2 = oldEntries.Intersect(currentEntries, comparer).ToList();
                var intersectNew2 = currentEntries.Intersect(oldEntries, comparer).ToList();

                var intersectNew = intersectNew2.Except(intersectOld2, comparerQuantity).ToList().ToDictionary(comparer.GetHashCode);
                var intersectOld = intersectOld2.Except(intersectNew2, comparerQuantity).ToList().ToDictionary(comparer.GetHashCode);

                foreach (var differential in intersectOld)
                {
                    differential.Value.Cantitate = intersectNew[differential.Key].CantitateTarget - intersectNew[differential.Key].Cantitate;
                }

                foreach (var item1 in exceptDelete.Where(t => t.CantitateTarget > 0))
                {
                    item1.Cantitate = item1.CantitateTarget;
                }

                await tableStorageService.PrepareUpsert(exceptDelete.Concat(intersectOld.Values))
                                         .ExecuteBatch(ComandaVanzareEntry.GetProgressTableName());

                await tableStorageService.PrepareUpsert(intersectNew.Values)
                                        .Concat(tableStorageService.PrepareInsert(exceptAdd))
                                        .Concat(tableStorageService.PrepareDelete(exceptDelete))
                                        .ExecuteBatch();
                await CacheManager.Bust(typeof(ComandaVanzareEntry).Name, true, null);
                CacheManager.InvalidateOurs(typeof(ComandaVanzareEntry).Name);
            }
            await blobAccessStorageService.SetMetadata(syncName, null, new Dictionary<string, string>() { { "data_sync", when.ToUniversalTime().ToShortDateString() } });
        }

        private IEnumerable<ComandaVanzareEntry> MergeByHash(IEnumerable<ComandaVanzareEntry> list)
        {
            var comparer = ComandaVanzareEntry.GetEqualityComparer();
            foreach (var items in list.GroupBy(comparer.GetHashCode))
            {
                var sample = items.ElementAt(0);
                sample.Cantitate = items.Sum(t => t.Cantitate);
                if (items.Distinct(comparer).Count() > 1) throw new Exception("We fucked boyzs");
                yield return sample;
            }
        }

        public async Task<DateTime?> GetLastSyncDate()
        {
            var metadata = await blobAccessStorageService.GetMetadata(syncName);

            if (metadata.ContainsKey("data_sync"))
            {
                return DateTime.Parse(metadata["data_sync"]);
            }
            return null;
        }
    }
}
