using Azure.Data.Tables;
using AzureServices;
using EntityDto;
using ProjectKeys;
using RepositoryContract.Tickets;
using ServiceInterface;

namespace AzureTableRepository.Tickets
{
    public class TicketEntryRepository : ITicketEntryRepository
    {
        TableStorageService tableStorageService;
        ICacheManager<TicketEntity> CacheManagerTicket;
        ICacheManager<AttachmentEntry> CacheManagerAttachment;

        public TicketEntryRepository(ICacheManager<TicketEntity> CacheManagerTicket, ICacheManager<AttachmentEntry> CacheManagerAttachment)
        {
            tableStorageService = new TableStorageService();
            this.CacheManagerTicket = CacheManagerTicket;
            this.CacheManagerAttachment = CacheManagerAttachment;
        }

        public async Task<IList<TicketEntity>> GetAll(string tableName = null)
        {
            tableName = tableName ?? nameof(TicketEntity);
            return (await CacheManagerTicket.GetAll((from) => tableStorageService.Query<TicketEntity>(t => t.Timestamp > from, tableName).ToList(), tableName)).ToList();
        }

        public async Task Save(AttachmentEntry[] entry, string tableName = null)
        {
            tableName = tableName ?? nameof(AttachmentEntry);
            var from = DateTimeOffset.Now;
            await tableStorageService.PrepareUpsert(entry).ExecuteBatch(tableName);
            await CacheManagerAttachment.Bust(tableName, false, from);
            await CacheManagerAttachment.UpsertCache(tableName, entry);
        }

        public async Task Save(TicketEntity[] entries, string tableName = null)
        {
            tableName = tableName ?? nameof(TicketEntity);
            var from = DateTimeOffset.Now;
            await tableStorageService.PrepareUpsert(entries).ExecuteBatch(tableName);
            await CacheManagerTicket.Bust(tableName, false, from);
            await CacheManagerTicket.UpsertCache(tableName, entries);
        }

        public async Task<TicketEntity> GetTicket(string partitionKey, string rowKey, string tableName = null)
        {
            tableName = tableName ?? nameof(TicketEntity);
            TableClient tableClient = new(Environment.GetEnvironmentVariable(KeyCollection.StorageConnection), tableName, new TableClientOptions());
            await tableClient.CreateIfNotExistsAsync();
            var resp = await tableClient.GetEntityIfExistsAsync<TicketEntity>(partitionKey, rowKey);
            return resp.HasValue ? resp.Value! : null;
        }

        public async Task<IList<AttachmentEntry>> GetAllAttachments(string? partitionKey = null, string tableName = null)
        {
            tableName = tableName ?? nameof(AttachmentEntry);
            if (string.IsNullOrEmpty(partitionKey))
            {
                return (await CacheManagerAttachment.GetAll((from) => tableStorageService.Query<AttachmentEntry>(t => t.Timestamp > from, tableName).ToList(), tableName))
                    .Select(x => x.Shallowcopy<AttachmentEntry>()).ToList();
            }
            else
            {
                return tableStorageService.Query<AttachmentEntry>(t => t.PartitionKey == partitionKey, tableName).ToList();
            }
        }

        public async Task DeleteEntity<T>(T[] entities, string partitionKey = null, string tableName = null) where T : class, ITableEntryDto, ITableEntity
        {
            tableName = tableName ?? typeof(T).Name;
            await tableStorageService.PrepareDelete(entities).ExecuteBatch(tableName);

            if (typeof(T).Name == nameof(TicketEntity))
            {
                await CacheManagerTicket.RemoveFromCache(tableName, [.. entities.Cast<TicketEntity>()]);
                await CacheManagerTicket.Bust(tableName, true, null);
            }
            else if (typeof(T).Name == nameof(AttachmentEntry))
            {
                await CacheManagerAttachment.RemoveFromCache(tableName, [.. entities.Cast<AttachmentEntry>()]);
                await CacheManagerAttachment.Bust(tableName, true, null);
            }
        }
    }
}
