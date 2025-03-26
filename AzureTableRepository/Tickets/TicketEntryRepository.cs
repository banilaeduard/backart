using Azure.Data.Tables;
using AzureServices;
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

        public async Task<IList<TicketEntity>> GetAll()
        {
            return (await CacheManagerTicket.GetAll((from) => tableStorageService.Query<TicketEntity>(t => t.Timestamp > from).ToList())).Where(t => !t.IsDeleted).ToList();
        }

        public async Task Save(AttachmentEntry entry)
        {
            var from = DateTimeOffset.Now;
            await tableStorageService.Upsert(entry);
            await CacheManagerAttachment.Bust(typeof(AttachmentEntry).Name, false, from);
            await CacheManagerAttachment.UpsertCache(typeof(AttachmentEntry).Name, [entry]);
        }

        public async Task Save(TicketEntity[] entries)
        {
            var from = DateTimeOffset.Now;
            await tableStorageService.PrepareUpsert(entries).ExecuteBatch();
            await CacheManagerTicket.Bust(typeof(TicketEntity).Name, false, from);
            await CacheManagerTicket.UpsertCache(typeof(TicketEntity).Name, entries);
        }

        public async Task<TicketEntity> GetTicket(string partitionKey, string rowKey, string tableName = null)
        {
            tableName = tableName ?? nameof(TicketEntity);
            TableClient tableClient = new(Environment.GetEnvironmentVariable(KeyCollection.StorageConnection), tableName, new TableClientOptions());
            await tableClient.CreateIfNotExistsAsync();
            var resp = await tableClient.GetEntityIfExistsAsync<TicketEntity>(partitionKey, rowKey);
            return resp.HasValue ? resp.Value! : null;
        }

        public async Task<IList<AttachmentEntry>> GetAllAttachments(string? partitionKey = null)
        {
            if (string.IsNullOrEmpty(partitionKey))
            {
                return (await CacheManagerAttachment.GetAll((from) => tableStorageService.Query<AttachmentEntry>(t => t.Timestamp > from).ToList()))
                    .Select(x => x.Shallowcopy<AttachmentEntry>()).ToList();
            }
            else
            {
                return (await CacheManagerAttachment.GetAll((from) => tableStorageService.Query<AttachmentEntry>(t => t.PartitionKey == partitionKey).ToList(), nameof(AttachmentEntry) + $"_{partitionKey}"))
                    .Select(x => x.Shallowcopy<AttachmentEntry>()).ToList();
            }
        }
    }
}
