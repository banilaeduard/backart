using Azure.Data.Tables;
using AzureServices;
using EntityDto;
using Microsoft.Extensions.Logging;
using RepositoryContract.CommitedOrders;

namespace AzureTableRepository.CommitedOrders
{
    public class CommitedOrdersRepository : ICommitedOrdersRepository
    {
        TableStorageService tableStorageService;
        public CommitedOrdersRepository(ILogger<TableStorageService> logger)
        {
            tableStorageService = new TableStorageService(logger);
        }

        public async Task DeleteCommitedOrders(List<DispozitieLivrareEntry> items)
        {
            await tableStorageService.PrepareDelete(items.ToList()).ExecuteBatch();
            CacheManager<DispozitieLivrareEntry>.Bust();
        }

        public async Task<List<DispozitieLivrareEntry>> GetCommitedOrders(Func<DispozitieLivrareEntry, bool> expr)
        {
            return CacheManager<DispozitieLivrareEntry>.GetAll(() => tableStorageService.Query<DispozitieLivrareEntry>(t => true).ToList()).Where(expr).ToList();
        }

        public async Task<List<DispozitieLivrareEntry>> GetCommitedOrders()
        {
            return CacheManager<DispozitieLivrareEntry>.GetAll(() => tableStorageService.Query<DispozitieLivrareEntry>(t => true).ToList()).ToList();
        }

        public async Task InsertCommitedOrder(DispozitieLivrareEntry sample)
        {
            await tableStorageService.Insert(sample);
            CacheManager<DispozitieLivrareEntry>.Bust();
        }

        public async Task ImportCommitedOrders(IList<DispozitieLivrare> items)
        {
            var newEntries = items.GroupBy(t => t.NumarIntern).ToDictionary(t => t.Key);

            foreach (var groupedEntries in newEntries)
            {
                (IEnumerable<TableTransactionAction> items, TableStorageService self) transaction = ([], tableStorageService);
                var oldEntries = tableStorageService.Query<DispozitieLivrareEntry>(t => t.PartitionKey == groupedEntries.Key).ToList();
                transaction = transaction.Concat(tableStorageService.PrepareDelete(oldEntries.ToList()));

                foreach (var group in groupedEntries.Value.GroupBy(t => new { t.NumarIntern, t.CodProdus, t.CodLocatie, t.NumarComanda }))
                {
                    transaction = transaction.Concat(tableStorageService.PrepareInsert([DispozitieLivrareEntry.create(group.ElementAt(0), group.Sum(t => t.Cantitate))]));
                };

                if (transaction.items.Any())
                    await transaction.ExecuteBatch();
            }
            CacheManager<DispozitieLivrareEntry>.Bust();
        }
    }
}
