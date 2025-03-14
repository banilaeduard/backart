using EntityDto;

namespace RepositoryContract.CommitedOrders
{
    public interface ICommitedOrdersRepository
    {
        Task<List<DispozitieLivrareEntry>> GetCommitedOrder(int id);
        Task<List<DispozitieLivrareEntry>> GetCommitedOrders(int[] ids);
        Task<List<DispozitieLivrareEntry>> GetCommitedOrders(DateTime? from);
        Task<DateTime?> GetLastSyncDate();
        Task DeleteCommitedOrders(List<DispozitieLivrareEntry> items);
        Task InsertCommitedOrder(DispozitieLivrareEntry item);
        Task SetDelivered(int internalNumbers, int? numarAviz);
        Task ImportCommitedOrders(IList<DispozitieLivrare> items, DateTime when);
    }
}
