using Azure.Data.Tables;
using AzureServices;
using EntityDto;
using Microsoft.Extensions.Logging;
using RepositoryContract.CommitedOrders;

namespace AzureTableRepository.CommitedOrders
{
    public class CommitedOrdersRepository : ICommitedOrdersRepository
    {
        static BlobAccessStorageService blobAccessStorageService = new();
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
            return CacheManager<DispozitieLivrareEntry>.GetAll(() => tableStorageService.Query<DispozitieLivrareEntry>(t => !t.Livrata).ToList())
                .Where(t => expr(t) && !t.Livrata && t.StatusName == "Final").ToList();
        }

        public async Task<List<DispozitieLivrareEntry>> GetCommitedOrders()
        {
            return CacheManager<DispozitieLivrareEntry>.GetAll(() => tableStorageService.Query<DispozitieLivrareEntry>(t => !t.Livrata).ToList())
                .Where(t => !t.Livrata && t.StatusName == "Final").ToList();
        }

        public async Task InsertCommitedOrder(DispozitieLivrareEntry sample)
        {
            await tableStorageService.Insert(sample);
            CacheManager<DispozitieLivrareEntry>.Bust();
        }

        public async Task ImportCommitedOrders(IList<DispozitieLivrare> items)
        {
            if (items.Count == 0) return;
            var newEntries = items.GroupBy(t => t.NumarIntern).ToDictionary(t => t.Key);

            foreach (var groupedEntries in newEntries)
            {
                var oldEntries = tableStorageService.Query<DispozitieLivrareEntry>(t => t.PartitionKey == groupedEntries.Key).ToList();
                if (oldEntries.Count > 0 && oldEntries.Any(t => t.Livrata)) continue;

                (IEnumerable<TableTransactionAction> items, TableStorageService self) transaction = ([], tableStorageService);
                transaction = transaction.Concat(tableStorageService.PrepareDelete(oldEntries.ToList()));

                foreach (var group in groupedEntries.Value.GroupBy(t => new { t.NumarIntern, t.CodProdus, t.CodLocatie, t.NumarComanda }))
                {
                    transaction = transaction.Concat(tableStorageService.PrepareInsert([DispozitieLivrareEntry.create(group.ElementAt(0), group.Sum(t => t.Cantitate))]));
                };

                if (transaction.items.Any())
                    await transaction.ExecuteBatch();
            }

            var latest = newEntries.ElementAt(newEntries.Count / 2).Value.ElementAt(0).DataDocument;
            blobAccessStorageService.SetMetadata("sync_control/LastSyncDate", new() { { "data_sync", latest.ToUniversalTime().ToShortDateString() } });
            CacheManager<DispozitieLivrareEntry>.Bust();
        }

        public async Task SetDelivered(int internalNumber)
        {
            var entries = tableStorageService.Query<DispozitieLivrareEntry>(t => t.PartitionKey == internalNumber.ToString()).ToList();
            foreach (var entry in entries) entry.Livrata = true;

            await tableStorageService.PrepareUpsert(entries).ExecuteBatch();
            foreach (var cached in (await GetCommitedOrders()).Where(t => t.PartitionKey == internalNumber.ToString()))
            {
                var entry = entries.FirstOrDefault(t => t.RowKey == cached.RowKey);
                if (entry != null)
                {
                    cached.Livrata = true;
                    cached.ETag = entry.ETag;
                }
            }
        }

        public async Task<DateTime?> GetLastSyncDate()
        {
            blobAccessStorageService.Check("sync_control/LastSyncDate");
            var metadata = blobAccessStorageService.GetMetadata("sync_control/LastSyncDate");

            if (metadata.ContainsKey("data_sync"))
            {
                return DateTime.Parse(metadata["data_sync"]);
            }
            return null;
        }
    }
}
