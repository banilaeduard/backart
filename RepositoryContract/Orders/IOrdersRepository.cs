using EntityDto.CommitedOrders;

namespace RepositoryContract.Orders
{
    public interface IOrdersRepository
    {
        Task ImportOrders(IList<Order> items, DateTime when);
        Task<List<OrderEntry>> GetOrders(Func<OrderEntry, bool> expr, string? table = null);
        Task<List<OrderEntry>> GetOrders(string? table = null);
        Task<DateTime?> GetLastSyncDate();
    }
}
