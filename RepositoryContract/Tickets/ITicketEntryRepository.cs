using Azure.Data.Tables;

namespace RepositoryContract.Tickets
{
    public interface ITicketEntryRepository
    {
        Task<IList<TicketEntity>> GetAll();
        Task Save(AttachmentEntry entry);
        Task Save(TicketEntity[] entry);
        Task Delete<T>(string partitionKey, string rowKey) where T : class, ITableEntity;
        Task<T?> GetIfExists<T>(string partitionKey, string rowKey) where T : class, ITableEntity;
    }
}
