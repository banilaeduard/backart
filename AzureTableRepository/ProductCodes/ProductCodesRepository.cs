using Azure.Data.Tables;
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
            return CacheManager.GetAll((from) =>
                    tableStorageService.Query<ProductCodeEntry>(t => t.Timestamp >= from, table).ToList()
                    , table).Select(t => t.Shallowcopy())
                .Where(expr).ToList();
        }

        public async Task<IList<ProductCodeEntry>> GetProductCodes(string? table = null)
        {
            return CacheManager.GetAll((from) =>
                    tableStorageService.Query<ProductCodeEntry>(t => t.Timestamp >= from, table).ToList()
                    , table).Select(t => t.Shallowcopy()).ToList();
        }

        public async Task Delete(string partitionKey, string rowKey)
        {
            var item = tableStorageService.Query<ProductCodeEntry>(t => t.PartitionKey == partitionKey && t.RowKey == rowKey).First();

            var batch = tableStorageService.PrepareDelete([item]);
            await DeleteRecursive(item, batch);

            await batch.ExecuteBatch();
            CacheManager.RemoveFromCache(typeof(ProductCodeEntry).Name, batch.items.Select(t => t.Entity).ToList());
            CacheManager.Bust(typeof(ProductCodeEntry).Name, true, null);
        }

        private async Task DeleteRecursive(ProductCodeEntry entry, (List<TableTransactionAction> items, TableStorageService self) batch)
        {
            var relatedEntities = await GetProductCodes(t => t.ParentCode == entry.Code && t.RootCode == entry.RootCode);

            foreach (var entity in relatedEntities)
            {
                if (entity.PartitionKey == entry.PartitionKey && entity.RowKey == entry.RowKey) continue;
                batch.Concat(tableStorageService.PrepareDelete([entity]));
                await DeleteRecursive(entity, batch);
            }
        }
    }
}
