using Azure.Data.Tables;

namespace RepositoryContract.Tickets
{
    public interface ITicketEntryRepository
    {
        Task<IList<TicketEntity>> GetAll(int page, int pageSize);
        Task Save(AttachmentEntry entry);
        Task Save(TicketEntity entry);
        Task Delete<T>(string partitionKey, string rowKey) where T : class, ITableEntity;
        Task<bool> Exists<T>(string partitionKey, string rowKey) where T : class, ITableEntity;
    }
}
