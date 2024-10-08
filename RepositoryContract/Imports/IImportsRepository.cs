using EntityDto;

namespace RepositoryContract.Imports
{
    public interface IImportsRepository
    {
        public Task<(IList<DispozitieLivrare> commited, IList<ComandaVanzare> orders)> GetImportCommitedOrders(DateTime? when = null, DateTime? when2 = null);
    }
}
