using AzureServices;
using EntityDto;
using Microsoft.Extensions.Logging;
using RepositoryContract.Orders;
using System.Linq.Expressions;

namespace AzureTableRepository.Orders
{
    public class OrdersRepository : IOrdersRepository
    {
        TableStorageService tableStorageService;
        public OrdersRepository(ILogger<TableStorageService> logger)
        {
            tableStorageService = new TableStorageService(logger);
        }
        public async Task<List<ComandaVanzareEntry>> GetOrders(string? table = null)
        {
            return tableStorageService.Query<ComandaVanzareEntry>(t => true, table).ToList();
        }

        public async Task<List<ComandaVanzareEntry>> GetOrders(Expression<Func<ComandaVanzareEntry, bool>> expr, string? table = null)
        {
            return tableStorageService.Query(expr, table).ToList();
        }

        public async Task ImportOrders(IList<ComandaVanzare> items)
        {
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
            }
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
    }
}
