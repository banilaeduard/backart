using Azure.Data.Tables;
using AzureSerRepositoryContract.ProductCodesvices;
using AzureServices;
using Microsoft.Extensions.Logging;
using RepositoryContract.Tickets;
using System.Collections.Concurrent;

namespace AzureTableRepository.Tickets
{
    public class TicketEntryRepository : ITicketEntryRepository
    {
        TableStorageService tableStorageService;

        public TicketEntryRepository(ILogger<TableStorageService> logger)
        {
            tableStorageService = new TableStorageService(logger);
        }

        public async Task Delete<T>(string partitionKey, string rowKey) where T : ITableEntity
        {
            await tableStorageService.Delete(partitionKey, rowKey, typeof(T).Name);
            CacheManager.Bust(typeof(TicketEntity).Name, true, null);
            CacheManager.RemoveFromCache(typeof(ProductCodeEntry).Name, [TableEntity.From(partitionKey, rowKey)]);
        }

        public async Task<IList<TicketEntity>> GetAll(int page, int pageSize)
        {
            return CacheManager.GetAll((from) => tableStorageService.Query<TicketEntity>(t => t.Timestamp >= from).ToList()).ToList();
        }

        public async Task Save(AttachmentEntry entry)
        {
            await tableStorageService.Upsert(entry);
            CacheManager.Bust(typeof(TicketEntity).Name, false, null);
            CacheManager.UpsertCache(typeof(ProductCodeEntry).Name, [entry]);
        }

        public async Task Save(TicketEntity entry)
        {
            await tableStorageService.Upsert(entry);
            CacheManager.Bust(typeof(TicketEntity).Name, false, null);
            CacheManager.UpsertCache(typeof(ProductCodeEntry).Name, [entry]);
        }

        public async Task<bool> Exists<T>(string partitionKey, string rowKey) where T : ITableEntity
        {
            var tableName = typeof(T).Name;
            TableClient tableClient = new(Environment.GetEnvironmentVariable("storage_connection"), tableName, new TableClientOptions());
            tableClient.CreateIfNotExists();
            return tableClient.GetEntityIfExists<TicketEntity>(partitionKey, rowKey).HasValue;
        }
    }
}
