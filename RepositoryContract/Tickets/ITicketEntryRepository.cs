using Azure.Data.Tables;
using EntityDto;

namespace RepositoryContract.Tickets
{
    public interface ITicketEntryRepository
    {
        Task<IList<TicketEntity>> GetAll(string tableName = null);
        Task<IList<AttachmentEntry>> GetAllAttachments(string? partitionKey = null, string tableName = null);
        Task Save(AttachmentEntry[] entry, string tableName = null);
        Task Save(TicketEntity[] entry, string tableName = null);
        Task<TicketEntity> GetTicket(string partitionKey, string rowKey, string tableName = null);
        Task DeleteEntity<T>(T[] entities, string partitionKey = null, string tableName = null) where T: class, ITableEntryDto, ITableEntity;
    }
}