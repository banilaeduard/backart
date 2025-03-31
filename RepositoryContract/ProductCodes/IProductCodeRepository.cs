using EntityDto;

namespace RepositoryContract.ProductCodes
{
    public interface IProductCodeRepository
    {
        Task<IList<ProductCodeEntry>> GetProductCodes(Func<ProductCodeEntry, bool> expr);
        Task UpsertCodes(ProductCodeEntry[] productCodes);
        Task<IList<ProductCodeEntry>> GetProductCodes();
        Task<IList<ProductStatsEntry>> GetProductStats();
        Task<IList<ProductStatsEntry>> CreateProductStats(IList<ProductStatsEntry> productStats);
        Task<IList<ProductCodeStatsEntry>> CreateProductCodeStatsEntry(IList<ProductCodeStatsEntry> productStats);
        Task<IList<ProductCodeStatsEntry>> GetProductCodeStatsEntry();
        Task Delete<T>(T entity) where T : ITableEntryDto;
    }
}
