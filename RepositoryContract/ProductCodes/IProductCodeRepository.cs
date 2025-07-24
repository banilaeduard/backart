using EntityDto;
using RepositoryContract.Cfg;

namespace RepositoryContract.ProductCodes
{
    public interface IProductCodeRepository
    {
        Task<IList<ProductClientCode>> GetProductClientCodes(string clientName);
        Task<IList<ProductCodeEntry>> GetProductCodes(Func<ProductCodeEntry, bool> expr);
        Task UpsertCodes(ProductCodeEntry[] productCodes);
        Task<IList<ProductCodeEntry>> GetProductCodes();
        Task<IList<ProductStatsEntry>> GetProductStats();
        Task<IList<ProductStatsEntry>> CreateProductStats(IList<ProductStatsEntry> productStats);
        Task<IList<ProductCodeStatsEntry>> CreateProductCodeStatsEntry(IList<ProductCodeStatsEntry> productStats);
        Task<IList<ProductCodeStatsEntry>> GetProductCodeStatsEntry();
        Task<IList<CategoryEntity>> GetCategory(string filter = null);
        Task Delete<T>(T entity) where T : ITableEntryDto;
    }
}
