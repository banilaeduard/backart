using AzureServices;
using EntityDto;
using RepositoryContract.ProductCodes;
using ServiceInterface;

namespace AzureTableRepository.ProductCodes
{
    public class ProductCodesRepository : IProductCodeRepository
    {
        TableStorageService tableStorageService;
        ICacheManager<ProductCodeEntry> CacheManagerProductCodeEntry;
        ICacheManager<ProductStatsEntry> CacheManagerProductStatsEntry;
        ICacheManager<ProductCodeStatsEntry> CacheManagerProductCodeStatsEntry;

        public ProductCodesRepository(
            ICacheManager<ProductCodeEntry> CacheManagerProductCodeEntry,
            ICacheManager<ProductStatsEntry> CacheManagerProductStatsEntry,
            ICacheManager<ProductCodeStatsEntry> CacheManagerProductCodeStatsEntry
            )
        {
            tableStorageService = new TableStorageService();
            this.CacheManagerProductCodeEntry = CacheManagerProductCodeEntry;
            this.CacheManagerProductStatsEntry = CacheManagerProductStatsEntry;
            this.CacheManagerProductCodeStatsEntry = CacheManagerProductCodeStatsEntry;
        }

        public async Task<IList<ProductCodeEntry>> GetProductCodes(Func<ProductCodeEntry, bool> expr)
        {
            return (await CacheManagerProductCodeEntry.GetAll((from) =>
                    tableStorageService.Query<ProductCodeEntry>(t => t.Timestamp > from, nameof(ProductCodeEntry)).ToList()
                    , nameof(ProductCodeEntry))).Select(t => t.Shallowcopy<ProductCodeEntry>())
                .Where(expr).ToList();
        }

        public async Task<IList<ProductCodeEntry>> GetProductCodes()
        {
            return (await CacheManagerProductCodeEntry.GetAll((from) =>
                    tableStorageService.Query<ProductCodeEntry>(t => t.Timestamp > from, nameof(ProductCodeEntry)).ToList()
                    , nameof(ProductCodeEntry))).Select(t => t.Shallowcopy<ProductCodeEntry>()).ToList();
        }

        public async Task Delete<T>(T entity) where T : ITableEntryDto
        {
            if (typeof(ProductCodeEntry).IsAssignableFrom(typeof(T)))
            {
                var item = tableStorageService.Query<ProductCodeEntry>(t => t.PartitionKey == entity.PartitionKey && t.RowKey == entity.RowKey).First();
                List<ProductCodeEntry> deltedItems = [.. await GetProductCodes(t => t.RootCode == item.RootCode)];

                await tableStorageService.PrepareDelete(deltedItems).ExecuteBatch(entity.GetType().Name);
                await CacheManagerProductCodeEntry.RemoveFromCache(nameof(ProductCodeEntry), deltedItems);
                await CacheManagerProductCodeEntry.Bust(nameof(ProductCodeEntry), true, null);
            }
        }

        public async Task<IList<ProductStatsEntry>> GetProductStats()
        {
            return (await CacheManagerProductStatsEntry.GetAll((from) =>
                    tableStorageService.Query<ProductStatsEntry>(t => t.Timestamp > from, nameof(ProductStatsEntry)).ToList()
                    , nameof(ProductStatsEntry))).Select(t => t.Shallowcopy<ProductStatsEntry>()).ToList();
        }

        public async Task<IList<ProductStatsEntry>> CreateProductStats(IList<ProductStatsEntry> productStats)
        {
            DateTimeOffset from = DateTimeOffset.Now;
            await tableStorageService.PrepareUpsert(productStats).ExecuteBatch();
            await CacheManagerProductStatsEntry.Bust(typeof(ProductStatsEntry).Name, false, from);
            await CacheManagerProductStatsEntry.UpsertCache(typeof(ProductStatsEntry).Name, [.. productStats]);
            return productStats;
        }

        public async Task<IList<ProductCodeStatsEntry>> CreateProductCodeStatsEntry(IList<ProductCodeStatsEntry> productStats)
        {
            DateTimeOffset from = DateTimeOffset.Now;
            await tableStorageService.PrepareUpsert(productStats).ExecuteBatch();
            await CacheManagerProductCodeStatsEntry.Bust(typeof(ProductCodeStatsEntry).Name, false, from);
            await CacheManagerProductCodeStatsEntry.UpsertCache(typeof(ProductCodeStatsEntry).Name, [.. productStats]);
            return productStats;
        }

        public async Task<IList<ProductCodeStatsEntry>> GetProductCodeStatsEntry()
        {
            return (await CacheManagerProductCodeStatsEntry.GetAll((from) =>
                    tableStorageService.Query<ProductCodeStatsEntry>(t => t.Timestamp > from, nameof(ProductCodeStatsEntry)).ToList()
                    , nameof(ProductCodeStatsEntry))).Select(t => t.Shallowcopy<ProductCodeStatsEntry>()).ToList();
        }

        public async Task UpsertCodes(ProductCodeEntry[] productCodes)
        {
            DateTimeOffset from = DateTimeOffset.Now;
            await tableStorageService.PrepareUpsert(productCodes).ExecuteBatch();
            await CacheManagerProductCodeEntry.Bust(nameof(ProductCodeEntry), false, from);
            await CacheManagerProductCodeEntry.UpsertCache(nameof(ProductCodeEntry), [.. productCodes]);
        }
    }
}
