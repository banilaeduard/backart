using Azure.Data.Tables;
using AzureServices;
using EntityDto.CommitedOrders;
using RepositoryContract.CommitedOrders;
using ServiceInterface;
using ServiceInterface.Storage;

namespace AzureTableRepository.CommitedOrders
{
    public class CommitedOrdersRepository : ICommitedOrdersRepository
    {
        static readonly string syncName = $"sync_control_LastSyncDate_${nameof(CommitedOrderEntry)}";
        IMetadataService metadataService;
        ICacheManager<CommitedOrderEntry> CacheManager;
        TableStorageService tableStorageService;
        public CommitedOrdersRepository(ICacheManager<CommitedOrderEntry> CacheManager, IMetadataService metadataService)
        {
            tableStorageService = new TableStorageService();
            this.CacheManager = CacheManager;
            this.metadataService = metadataService;
        }

        public async Task DeleteCommitedOrders(List<CommitedOrderEntry> items)
        {
            var toRemove = tableStorageService.PrepareDelete(items.ToList());
            await toRemove.ExecuteBatch();

            await CacheManager.Bust(nameof(CommitedOrderEntry), true, null);
            await CacheManager.RemoveFromCache(nameof(CommitedOrderEntry), toRemove.items.Select(t => (CommitedOrderEntry)t.Entity).ToList());
        }

        public async Task<List<CommitedOrderEntry>> GetCommitedOrders(int[] ids)
        {
            return (await CacheManager.GetAll((from) => tableStorageService.Query<CommitedOrderEntry>(t => t.Timestamp > from).ToList()))
                               .Where(t => ids.Contains(int.Parse(t.PartitionKey))).ToList();
        }

        public async Task<List<CommitedOrderEntry>> GetCommitedOrders(DateTime? _from)
        {
            return (await CacheManager.GetAll((from) => tableStorageService.Query<CommitedOrderEntry>(t => t.Timestamp > from).ToList())).Where(t => t.DataDocument > _from).ToList();
        }

        public async Task InsertCommitedOrder(CommitedOrderEntry sample)
        {
            var offset = DateTimeOffset.Now;
            await tableStorageService.Insert(sample);

            await CacheManager.Bust(nameof(CommitedOrderEntry), false, offset);
            await CacheManager.UpsertCache(nameof(CommitedOrderEntry), [sample]);
        }

        public async Task ImportCommitedOrders(IList<CommitedOrder> items, DateTime when)
        {
            if (items.Count == 0) return;
            var newEntries = items.GroupBy(t => t.NumarIntern).ToDictionary(t => t.Key);

            foreach (var groupedEntries in newEntries)
            {
                var oldEntries = tableStorageService.Query<CommitedOrderEntry>(t => t.PartitionKey == groupedEntries.Key).ToList();
                if (oldEntries.Count > 0 && oldEntries.Any(t => t.Livrata)) continue;

                (List<TableTransactionAction> items, TableStorageService self) transaction = ([], tableStorageService);
                transaction = transaction.Concat(tableStorageService.PrepareDelete(oldEntries.ToList()));

                foreach (var group in groupedEntries.Value.GroupBy(t => new { t.NumarIntern, t.CodProdus, t.CodLocatie, t.NumarComanda }))
                {
                    var sample = group.ElementAt(0);
                    transaction = transaction.Concat(tableStorageService.PrepareInsert([CommitedOrderEntry.create(sample, group.Sum(t => t.Cantitate), group.Sum(t => t.Cantitate) * group.Sum(t => t.Greutate ?? 0))]));
                };

                if (transaction.items.Any())
                {
                    var offset = DateTimeOffset.Now;
                    await transaction.ExecuteBatch();
                    await CacheManager.Bust(nameof(CommitedOrderEntry), transaction.items.Where(t => t.ActionType == TableTransactionActionType.Delete).Any(), offset);
                    await CacheManager.RemoveFromCache(nameof(CommitedOrderEntry),
                        transaction.items.Where(t => t.ActionType == TableTransactionActionType.Delete).Select(t => (CommitedOrderEntry)t.Entity).ToList());
                    await CacheManager.UpsertCache(nameof(CommitedOrderEntry),
                        transaction.items.Where(t => t.ActionType != TableTransactionActionType.Delete).Select(t => (CommitedOrderEntry)t.Entity).ToList());
                }
            }

            await metadataService.SetMetadata(syncName, null, new Dictionary<string, string>() { { "data_sync", when.ToUniversalTime().ToShortDateString() } });
        }

        public async Task SetDelivered(int internalNumber, int? numarAviz)
        {
            var entries = tableStorageService.Query<CommitedOrderEntry>(t => t.PartitionKey == internalNumber.ToString()).ToList();
            foreach (var entry in entries)
            {
                entry.Livrata = true;
                entry.NumarAviz = numarAviz;
                entry.TransportStatus = "Delivered";
                entry.TransportDate = DateTime.Now;
            }

            var transactions = tableStorageService.PrepareUpsert(entries);

            var offset = DateTimeOffset.Now;
            await transactions.ExecuteBatch();

            await CacheManager.Bust(nameof(CommitedOrderEntry), false, offset);
            await CacheManager.UpsertCache(nameof(CommitedOrderEntry), transactions.items.Select(t => (CommitedOrderEntry)t.Entity).ToList());
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

        public async Task<List<CommitedOrderEntry>> GetCommitedOrder(int id)
        {
            return tableStorageService.Query<CommitedOrderEntry>(t => t.PartitionKey == id.ToString()).ToList();
        }
    }
}
