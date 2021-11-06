using Azure.Data.Tables;
using AzureServices;
using EntityDto;
using Microsoft.Extensions.Logging;
using RepositoryContract.CommitedOrders;

namespace AzureTableRepository.CommitedOrders
{
    public class CommitedOrdersRepository : ICommitedOrdersRepository
    {
        static readonly string syncName = $"sync_control/LastSyncDate_${nameof(DispozitieLivrareEntry)}";
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

            CacheManager.Bust(nameof(DispozitieLivrareEntry), true, null);
            CacheManager.RemoveFromCache(nameof(DispozitieLivrareEntry), toRemove.items.Select(t => t.Entity).ToList());
        }

        public async Task<List<DispozitieLivrareEntry>> GetCommitedOrders(Func<DispozitieLivrareEntry, bool> expr)
        {
            return CacheManager.GetAll((from) => tableStorageService.Query<DispozitieLivrareEntry>(t => t.Timestamp > from).ToList()).Where(expr).ToList();
        }

        public async Task<List<DispozitieLivrareEntry>> GetCommitedOrders()
        {
            return CacheManager.GetAll((from) => tableStorageService.Query<DispozitieLivrareEntry>(t => t.Timestamp > from).ToList()).ToList();
        }

        public async Task InsertCommitedOrder(DispozitieLivrareEntry sample)
        {
            var offset = DateTimeOffset.Now;
            await tableStorageService.Insert(sample);

            CacheManager.Bust(nameof(DispozitieLivrareEntry), false, offset);
            CacheManager.UpsertCache(nameof(DispozitieLivrareEntry), [sample]);
        }

        public async Task ImportCommitedOrders(IList<DispozitieLivrare> items, DateTime when)
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
                    CacheManager.Bust(nameof(DispozitieLivrareEntry), transaction.items.Where(t => t.ActionType == TableTransactionActionType.Delete).Any(), offset);
                    CacheManager.RemoveFromCache(nameof(DispozitieLivrareEntry),
                        transaction.items.Where(t => t.ActionType == TableTransactionActionType.Delete).Select(t => t.Entity).ToList());
                    CacheManager.UpsertCache(nameof(DispozitieLivrareEntry),
                        transaction.items.Where(t => t.ActionType != TableTransactionActionType.Delete).Select(t => t.Entity).ToList());
                }
            }

            blobAccessStorageService.SetMetadata(syncName, null, new Dictionary<string, string>() { { "data_sync", when.ToUniversalTime().ToShortDateString() } });
        }

        public async Task SetDelivered(int internalNumber)
        {
            var entries = tableStorageService.Query<DispozitieLivrareEntry>(t => t.PartitionKey == internalNumber.ToString()).ToList();
            foreach (var entry in entries) entry.Livrata = true;

            var transactions = tableStorageService.PrepareUpsert(entries);

            var offset = DateTimeOffset.Now;
            await transactions.ExecuteBatch();

            CacheManager.Bust(nameof(DispozitieLivrareEntry), false, offset);
            CacheManager.UpsertCache(nameof(DispozitieLivrareEntry), transactions.items.Select(t => t.Entity).ToList());
        }

        public async Task<DateTime?> GetLastSyncDate()
        {
            var metadata = blobAccessStorageService.GetMetadata(syncName);

            if (metadata.ContainsKey("data_sync"))
            {
                return DateTime.Parse(metadata["data_sync"]);
            }
            return null;
        }
    }
}
