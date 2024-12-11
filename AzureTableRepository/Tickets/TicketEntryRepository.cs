using Azure.Data.Tables;
using AzureServices;
using Microsoft.Extensions.Logging;
using RepositoryContract;
using RepositoryContract.Tickets;

namespace AzureTableRepository.Tickets
{
    public class TicketEntryRepository : ITicketEntryRepository
    {
        TableStorageService tableStorageService;

        public TicketEntryRepository()
        {
            tableStorageService = new TableStorageService();
        }

        public async Task Delete<T>(string partitionKey, string rowKey) where T : class, ITableEntity
        {
            await tableStorageService.Delete(partitionKey, rowKey, typeof(T).Name);
            CacheManager.Bust(typeof(T).Name, true, null);
            CacheManager.RemoveFromCache(typeof(T).Name, [TableEntityPK.From(partitionKey, rowKey)]);
        }

        public async Task<IList<TicketEntity>> GetAll()
        {
            return CacheManager.GetAll((from) => tableStorageService.Query<TicketEntity>(t => t.Timestamp > from).ToList()).Where(t => !t.IsDeleted).ToList();
        }

        public async Task Save(AttachmentEntry entry)
        {
            var from = DateTimeOffset.Now;
            await tableStorageService.Upsert(entry);
            CacheManager.Bust(typeof(AttachmentEntry).Name, false, from);
            CacheManager.UpsertCache(typeof(AttachmentEntry).Name, [entry]);
        }

        public async Task Save(TicketEntity[] entries)
        {
            var from = DateTimeOffset.Now;
            await tableStorageService.PrepareUpsert(entries).ExecuteBatch();
            CacheManager.Bust(typeof(TicketEntity).Name, false, from);
            CacheManager.UpsertCache(typeof(TicketEntity).Name, entries);
        }

        public async Task<T?> GetIfExists<T>(string partitionKey, string rowKey) where T : class, ITableEntity
        {
            var tableName = typeof(T).Name;
            TableClient tableClient = new(Environment.GetEnvironmentVariable("storage_connection"), tableName, new TableClientOptions());
            tableClient.CreateIfNotExists();
            var resp = tableClient.GetEntityIfExists<T>(partitionKey, rowKey);
            return resp.HasValue ? resp.Value! : null;
        }

        public async Task<IList<AttachmentEntry>> GetAllAttachments(string? partitionKey = null)
        {
            if (string.IsNullOrEmpty(partitionKey))
            {
                return CacheManager.GetAll((from) => tableStorageService.Query<AttachmentEntry>(t => t.Timestamp > from).ToList()).ToList();
            }
            else
            {
                return CacheManager.GetAll((from) => tableStorageService.Query<AttachmentEntry>(t => t.PartitionKey == partitionKey).ToList(), nameof(AttachmentEntry) + $"_{partitionKey}").ToList();
            }
        }
    }
}
