using EntityDto;

namespace RepositoryContract.Imports
{
    public interface IImportsRepository
    {
        public Task<(IList<DispozitieLivrare> commited, IList<ComandaVanzare> orders)> GetImportCommitedOrders(DateTime? when = null, DateTime? when2 = null);

        public Task<IList<DispozitieLivrare>> GetImportCommited(DateTime? when = null);

        public Task<IList<ComandaVanzare>> GetImportOrders(DateTime? when = null);

        public Task<(DateTime commited, DateTime order)> PollForNewContent();
    }
}
