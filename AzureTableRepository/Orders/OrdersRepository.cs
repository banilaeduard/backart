using AzureServices;
using EntityDto.CommitedOrders;
using RepositoryContract.Orders;
using ServiceInterface;
using ServiceInterface.Storage;

namespace AzureTableRepository.Orders
{
    public class OrdersRepository : IOrdersRepository
    {
        static readonly string syncName = $"sync_control_LastSyncDate_${typeof(OrderEntry).Name}";
        IMetadataService metadataService;
        ICacheManager<OrderEntry> CacheManager;
        TableStorageService tableStorageService;
        public OrdersRepository(ICacheManager<OrderEntry> CacheManager,IMetadataService metadataService)
        {
            tableStorageService = new TableStorageService();
            this.metadataService = metadataService;
            this.CacheManager = CacheManager;
        }
        public async Task<List<OrderEntry>> GetOrders(string? table = null)
        {
            table = table ?? typeof(OrderEntry).Name;
            return (await CacheManager.GetAll((from) => tableStorageService.Query<OrderEntry>(t => t.Timestamp > from).ToList(), table)).ToList();
        }

        public async Task<List<OrderEntry>> GetOrders(Func<OrderEntry, bool> expr, string? table = null)
        {
            table = table ?? typeof(OrderEntry).Name;
            return (await CacheManager.GetAll((from) => tableStorageService.Query<OrderEntry>(t => expr(t) && t.Timestamp > from).ToList(), table)).ToList();
        }

        public async Task ImportOrders(IList<Order> items, DateTime when)
        {
            if (items.Count == 0) return;
            var newEntries = items.Select(OrderEntry.create).GroupBy(OrderEntry.PKey).ToDictionary(t => t.Key, MergeByHash);

            foreach (var item in newEntries)
            {
                var oldEntries = tableStorageService.Query<OrderEntry>(t => t.PartitionKey == item.Key).ToList();
                var comparer = OrderEntry.GetEqualityComparer();
                var comparerQuantity = OrderEntry.GetEqualityComparer(true);

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
                                         .ExecuteBatch(OrderEntry.GetProgressTableName());

                await tableStorageService.PrepareUpsert(intersectNew.Values)
                                        .Concat(tableStorageService.PrepareInsert(exceptAdd))
                                        .Concat(tableStorageService.PrepareDelete(exceptDelete))
                                        .ExecuteBatch();
                await CacheManager.Bust(typeof(OrderEntry).Name, true, null);
                await CacheManager.InvalidateOurs(typeof(OrderEntry).Name);
            }
            await metadataService.SetMetadata(syncName, null, new Dictionary<string, string>() { { "data_sync", when.ToUniversalTime().ToShortDateString() } });
        }

        private IEnumerable<OrderEntry> MergeByHash(IEnumerable<OrderEntry> list)
        {
            var comparer = OrderEntry.GetEqualityComparer();
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
            var metadata = await metadataService.GetMetadata(syncName);

            if (metadata.ContainsKey("data_sync"))
            {
                return DateTime.Parse(metadata["data_sync"]);
            }
            return null;
        }
    }
}
