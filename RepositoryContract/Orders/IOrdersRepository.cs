using EntityDto;

namespace RepositoryContract.Orders
{
    public interface IOrdersRepository
    {
        Task ImportOrders(IList<ComandaVanzare> items, DateTime when);
        Task<List<ComandaVanzareEntry>> GetOrders(Func<ComandaVanzareEntry, bool> expr, string? table = null);
        Task<List<ComandaVanzareEntry>> GetOrders(string? table = null);
        Task<DateTime?> GetLastSyncDate();
    }
}
