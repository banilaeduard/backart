using EntityDto.CommitedOrders;

namespace RepositoryContract.CommitedOrders
{
    public interface ICommitedOrdersRepository
    {
        Task<List<CommitedOrderEntry>> GetCommitedOrder(int id);
        Task<List<CommitedOrderEntry>> GetCommitedOrders(int[] ids);
        Task<List<CommitedOrderEntry>> GetCommitedOrders(DateTime? from);
        Task<DateTime?> GetLastSyncDate();
        Task DeleteCommitedOrders(List<CommitedOrderEntry> items);
        Task InsertCommitedOrder(CommitedOrderEntry item);
        Task SetDelivered(int internalNumbers, int? numarAviz);
        Task ImportCommitedOrders(IList<CommitedOrder> items, DateTime when);
    }
}
