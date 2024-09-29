using AzureSerRepositoryContract.ProductCodesvices;
using AzureServices;
using Microsoft.Extensions.Logging;
using RepositoryContract.ProductCodes;

namespace AzureTableRepository.ProductCodes
{
    public class ProductCodesRepository : IProductCodeRepository
    {
        TableStorageService tableStorageService;

        public ProductCodesRepository(ILogger<TableStorageService> logger)
        {
            tableStorageService = new TableStorageService(logger);
        }

        public async Task<IList<ProductCodeEntry>> GetProductCodes(Func<ProductCodeEntry, bool> expr, string? table = null)
        {
            return CacheManager<ProductCodeEntry>.GetAll(() =>
                    tableStorageService.Query<ProductCodeEntry>(t => true, table).Select(t => t.Shallowcopy()).ToList()
                    , table)
                .Where(expr).ToList();
        }

        public async Task<IList<ProductCodeEntry>> GetProductCodes(string? table = null)
        {
            return CacheManager<ProductCodeEntry>.GetAll(() =>
                    tableStorageService.Query<ProductCodeEntry>(t => true, table).Select(t => t.Shallowcopy()).ToList()
                    , table);
        }
    }
}
