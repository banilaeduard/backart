using EntityDto;

namespace RepositoryContract.Imports
{
    public interface IImportsRepository
    {
        public Task<IList<ComandaVanzare>> GetImportOrders();
    }
}
