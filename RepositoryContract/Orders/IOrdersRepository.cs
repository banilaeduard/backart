using EntityDto;
using System.Linq.Expressions;

namespace RepositoryContract.Orders
{
    public interface IOrdersRepository
    {
        Task ImportOrders(IList<ComandaVanzare> items);
        Task<List<ComandaVanzareEntry>> GetOrders(Expression<Func<ComandaVanzareEntry, bool>> expr);
        Task<List<ComandaVanzareEntry>> GetOrders();
    }
}
