using Azure.Data.Tables;
using AzureServices;
using Microsoft.Extensions.Logging;
using RepositoryContract.Tickets;

namespace AzureTableRepository.Tickets
{
    public class TicketEntryRepository : ITicketEntryRepository
    {
        TableStorageService tableStorageService;

        public TicketEntryRepository(ILogger<TableStorageService> logger)
        {
            tableStorageService = new TableStorageService(logger);
        }

        public async Task Delete<T>(string partitionKey, string rowKey) where T : class, ITableEntity
        {
            await tableStorageService.Delete(partitionKey, rowKey, typeof(T).Name);
            CacheManager.Bust(typeof(T).Name, true, null);
            CacheManager.RemoveFromCache(typeof(T).Name, [TableEntity.From(partitionKey, rowKey)]);
        }

        public async Task<IList<TicketEntity>> GetAll(int page, int pageSize)
        {
            return CacheManager.GetAll((from) => tableStorageService.Query<TicketEntity>(t => t.Timestamp > from).ToList()).ToList();
        }

        public async Task Save(AttachmentEntry entry)
        {
            var from = DateTimeOffset.Now;
            await tableStorageService.Upsert(entry);
            CacheManager.Bust(typeof(AttachmentEntry).Name, false, from);
            CacheManager.UpsertCache(typeof(AttachmentEntry).Name, [entry]);
        }

        public async Task Save(TicketEntity entry)
        {
            var from = DateTimeOffset.Now;
            await tableStorageService.Upsert(entry);
            CacheManager.Bust(typeof(TicketEntity).Name, false, from);
            CacheManager.UpsertCache(typeof(TicketEntity).Name, [entry]);
        }

        public async Task<bool> Exists<T>(string partitionKey, string rowKey) where T : class, ITableEntity
        {
            var tableName = typeof(T).Name;
            TableClient tableClient = new(Environment.GetEnvironmentVariable("storage_connection"), tableName, new TableClientOptions());
            tableClient.CreateIfNotExists();
            return tableClient.GetEntityIfExists<T>(partitionKey, rowKey).HasValue;
        }
    }
}
