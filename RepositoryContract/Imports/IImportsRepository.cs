using EntityDto;

namespace RepositoryContract.Imports
{
    public interface IImportsRepository
    {
        public Task<IList<ComandaVanzare>> GetImportOrders(DateTime? when = null);
        public Task<IList<DispozitieLivrare>> GetImportCommitedOrders(DateTime? when = null);
    }
}
