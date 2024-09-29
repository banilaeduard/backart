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

        public async Task Delete<T>(string partitionKey, string rowKey) where T : ITableEntity
        {
            await tableStorageService.Delete(partitionKey, rowKey, typeof(T).Name);
            CacheManager<TicketEntity>.Bust();
        }

        public async Task<IList<TicketEntity>> GetAll(int page, int pageSize)
        {
            return CacheManager<TicketEntity>.GetAll(() => tableStorageService.Query<TicketEntity>(t => true).ToList()).ToList();
        }

        public async Task Save(AttachmentEntry entry)
        {
            await tableStorageService.Upsert(entry);
            CacheManager<TicketEntity>.Bust();
        }

        public async Task Save(TicketEntity entry)
        {
            await tableStorageService.Upsert(entry);
            CacheManager<TicketEntity>.Bust();
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
