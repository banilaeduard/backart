using System.Linq.Expressions;

namespace RepositoryContract.CommitedOrders
{
    public interface ICommitedOrdersRepository
    {
        Task<List<DispozitieLivrareEntry>> GetCommitedOrders(Expression<Func<DispozitieLivrareEntry, bool>> expr);
        Task<List<DispozitieLivrareEntry>> GetCommitedOrders();
        Task DeleteCommitedOrders(List<DispozitieLivrareEntry> items);
        Task InsertCommitedOrder(DispozitieLivrareEntry item);
    }
}
