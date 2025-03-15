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

        public ProductCodesRepository()
        {
            tableStorageService = new TableStorageService();
        }

        public async Task<IList<ProductCodeEntry>> GetProductCodes(Func<ProductCodeEntry, bool> expr, string? table = null)
        {
            return (await CacheManager.GetAll((from) =>
                    tableStorageService.Query<ProductCodeEntry>(t => t.Timestamp > from, table).ToList()
                    , table)).Select(t => t.Shallowcopy())
                .Where(expr).ToList();
        }

        public async Task<IList<ProductCodeEntry>> GetProductCodes(string? table = null)
        {
            return (await CacheManager.GetAll((from) =>
                    tableStorageService.Query<ProductCodeEntry>(t => t.Timestamp > from, table).ToList()
                    , table)).Select(t => t.Shallowcopy()).ToList();
        }

        public async Task Delete<T>(string partitionKey, string rowKey, string table = null)
        {
            if (typeof(ProductCodeEntry).IsAssignableFrom(typeof(T)))
            {
                var item = tableStorageService.Query<ProductCodeEntry>(t => t.PartitionKey == partitionKey && t.RowKey == rowKey).First();

                var batch = tableStorageService.PrepareDelete([item]);
                await DeleteRecursive(item, batch);

                await batch.ExecuteBatch(table);
                CacheManager.RemoveFromCache(typeof(ProductCodeEntry).Name, batch.items.Select(t => t.Entity).ToList());
                await CacheManager.Bust(typeof(ProductCodeEntry).Name, true, null);
            }
            if (typeof(ProductStatsEntry).IsAssignableFrom(typeof(T)))
            {

            }
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

        public async Task<IList<ProductStatsEntry>> GetProductStats(string? table = null)
        {
            return (await CacheManager.GetAll((from) =>
                    tableStorageService.Query<ProductStatsEntry>(t => t.Timestamp > from, table).ToList()
                    , table)).Select(t => t.Shallowcopy()).ToList();
        }

        public async Task<IList<ProductStatsEntry>> CreateProductStats(IList<ProductStatsEntry> productStats, string? table = null)
        {
            DateTimeOffset from = DateTimeOffset.Now;
            await tableStorageService.PrepareUpsert(productStats).ExecuteBatch();
            await CacheManager.Bust(typeof(ProductStatsEntry).Name, false, from);
            CacheManager.UpsertCache(typeof(ProductStatsEntry).Name, [.. productStats]);
            return productStats;
        }

        public async Task<IList<ProductCodeStatsEntry>> CreateProductCodeStatsEntry(IList<ProductCodeStatsEntry> productStats, string? table = null)
        {
            DateTimeOffset from = DateTimeOffset.Now;
            await tableStorageService.PrepareUpsert(productStats).ExecuteBatch();
            await CacheManager.Bust(typeof(ProductCodeStatsEntry).Name, false, from);
            CacheManager.UpsertCache(typeof(ProductCodeStatsEntry).Name, [.. productStats]);
            return productStats;
        }

        public async Task<IList<ProductCodeStatsEntry>> GetProductCodeStatsEntry(string? table = null)
        {
            return (await CacheManager.GetAll((from) =>
                    tableStorageService.Query<ProductCodeStatsEntry>(t => t.Timestamp > from, table).ToList()
                    , table)).Select(t => t.Shallowcopy()).ToList();
        }
    }
}
