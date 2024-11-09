using AzureSerRepositoryContract.ProductCodesvices;

namespace RepositoryContract.ProductCodes
{
    public interface IProductCodeRepository
    {
        Task<IList<ProductCodeEntry>> GetProductCodes(Func<ProductCodeEntry, bool> expr, string? table = null);
        Task<IList<ProductCodeEntry>> GetProductCodes(string? table = null);
        Task<IList<ProductStatsEntry>> GetProductStats(string? table = null); 
        Task<IList<ProductStatsEntry>> CreateProductStats(IList<ProductStatsEntry> productStats, string? table = null);
        Task<IList<ProductCodeStatsEntry>> CreateProductCodeStatsEntry(IList<ProductCodeStatsEntry> productStats, string? table = null);
        Task<IList<ProductCodeStatsEntry>> GetProductCodeStatsEntry(string? table = null);
        Task Delete<T>(string partitionKey, string rowKey, string tableName = null);
    }
}
