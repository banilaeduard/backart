using Azure.Data.Tables;

namespace RepositoryContract.Tickets
{
    public interface ITicketEntryRepository
    {
        Task<IList<TicketEntity>> GetAll();
        Task<IList<AttachmentEntry>> GetAllAttachments(string? partitionKey = null);
        Task Save(AttachmentEntry entry);
        Task Save(TicketEntity[] entry);
        Task<TicketEntity> GetTicket(string partitionKey, string rowKey, string tableName = null);
        Task DeleteEntity<T>(T[] entities, string partitionKey = null, string tableName = null) where T: class, ITableEntity;
    }
}