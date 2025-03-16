using EntityDto.CommitedOrders;

namespace RepositoryContract.Imports
{
    public interface IImportsRepository
    {
        public Task<(IList<CommitedOrder> commited, IList<Order> orders)> GetImportCommitedOrders(DateTime? when = null, DateTime? when2 = null);

        public Task<IList<CommitedOrder>> GetImportCommited(DateTime? when = null);

        public Task<IList<Order>> GetImportOrders(DateTime? when = null);

        public Task<(DateTime commited, DateTime order)> PollForNewContent();
    }
}
