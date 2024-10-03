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
            var toRemove = tableStorageService.PrepareDelete(items.ToList());
            await toRemove.ExecuteBatch();

            CacheManager.Bust(typeof(DispozitieLivrareEntry).Name, true, null);
            CacheManager.RemoveFromCache(typeof(DispozitieLivrareEntry).Name, toRemove.items.Select(t => t.Entity).ToList());
        }

        public async Task<List<DispozitieLivrareEntry>> GetCommitedOrders(Func<DispozitieLivrareEntry, bool> expr)
        {
            return CacheManager.GetAll((from) => tableStorageService.Query<DispozitieLivrareEntry>(t => t.Timestamp > from && !t.Livrata).ToList())
                .Where(t => expr(t) && !t.Livrata && t.StatusName == "Final").ToList();
        }

        public async Task<List<DispozitieLivrareEntry>> GetCommitedOrders()
        {
            return CacheManager.GetAll((from) => tableStorageService.Query<DispozitieLivrareEntry>(t => t.Timestamp > from && !t.Livrata).ToList())
                .Where(t => !t.Livrata && t.StatusName == "Final").ToList();
        }

        public async Task InsertCommitedOrder(DispozitieLivrareEntry sample)
        {
            var offset = DateTimeOffset.Now;
            await tableStorageService.Insert(sample);

            CacheManager.Bust(typeof(DispozitieLivrareEntry).Name, false, offset);
            CacheManager.UpsertCache(typeof(DispozitieLivrareEntry).Name, [sample]);
        }

        public async Task ImportCommitedOrders(IList<DispozitieLivrare> items)
        {
            if (items.Count == 0) return;
            var newEntries = items.GroupBy(t => t.NumarIntern).ToDictionary(t => t.Key);

            foreach (var groupedEntries in newEntries)
            {
                var oldEntries = tableStorageService.Query<DispozitieLivrareEntry>(t => t.PartitionKey == groupedEntries.Key).ToList();
                if (oldEntries.Count > 0 && oldEntries.Any(t => t.Livrata)) continue;

                (List<TableTransactionAction> items, TableStorageService self) transaction = ([], tableStorageService);
                transaction = transaction.Concat(tableStorageService.PrepareDelete(oldEntries.ToList()));

                foreach (var group in groupedEntries.Value.GroupBy(t => new { t.NumarIntern, t.CodProdus, t.CodLocatie, t.NumarComanda }))
                {
                    transaction = transaction.Concat(tableStorageService.PrepareInsert([DispozitieLivrareEntry.create(group.ElementAt(0), group.Sum(t => t.Cantitate))]));
                };

                if (transaction.items.Any())
                {
                    var offset = DateTimeOffset.Now;
                    await transaction.ExecuteBatch();
                    CacheManager.Bust(typeof(DispozitieLivrareEntry).Name, transaction.items.Where(t => t.ActionType == TableTransactionActionType.Delete).Any(), offset);
                    CacheManager.RemoveFromCache(typeof(DispozitieLivrareEntry).Name,
                        transaction.items.Where(t => t.ActionType == TableTransactionActionType.Delete).Select(t => t.Entity).ToList());
                    CacheManager.UpsertCache(typeof(DispozitieLivrareEntry).Name,
                        transaction.items.Where(t => t.ActionType != TableTransactionActionType.Delete).Select(t => t.Entity).ToList());
                }
            }

            var latest = newEntries.ElementAt(newEntries.Count / 2).Value.ElementAt(0).DataDocument;
            blobAccessStorageService.SetMetadata("sync_control/LastSyncDate", null, new Dictionary<string, string>() { { "data_sync", latest.ToUniversalTime().ToShortDateString() } });
        }

        public async Task SetDelivered(int internalNumber)
        {
            var entries = tableStorageService.Query<DispozitieLivrareEntry>(t => t.PartitionKey == internalNumber.ToString()).ToList();
            foreach (var entry in entries) entry.Livrata = true;

            var transactions = tableStorageService.PrepareUpsert(entries);

            var offset = DateTimeOffset.Now;
            await transactions.ExecuteBatch();

            CacheManager.Bust(typeof(DispozitieLivrareEntry).Name, false, offset);
            CacheManager.UpsertCache(typeof(DispozitieLivrareEntry).Name, transactions.items.Select(t => t.Entity).ToList());
        }

        public async Task<DateTime?> GetLastSyncDate()
        {
            var metadata = blobAccessStorageService.GetMetadata("sync_control/LastSyncDate");

            if (metadata.ContainsKey("data_sync"))
            {
                return DateTime.Parse(metadata["data_sync"]);
            }
            return null;
        }
    }
}
