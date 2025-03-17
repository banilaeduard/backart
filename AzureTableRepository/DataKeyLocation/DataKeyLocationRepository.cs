using AzureServices;
using RepositoryContract.DataKeyLocation;
using ServiceInterface;

namespace AzureTableRepository.DataKeyLocation
{
    public class DataKeyLocationRepository : IDataKeyLocationRepository
    {
        TableStorageService tableStorageService;
        ICacheManager<DataKeyLocationEntry> CacheManager;

        public DataKeyLocationRepository(ICacheManager<DataKeyLocationEntry> CacheManager)
        {
            tableStorageService = new TableStorageService();
            this.CacheManager = CacheManager;
        }

        public async Task DeleteLocation(DataKeyLocationEntry[] entries)
        {
            await tableStorageService.PrepareDelete(entries).ExecuteBatch();
            await CacheManager.Bust(typeof(DataKeyLocationEntry).Name, true, null);
            await CacheManager.RemoveFromCache(typeof(DataKeyLocationEntry).Name, entries);
        }

        public async Task<IList<DataKeyLocationEntry>> GetLocations()
        {
            return (await CacheManager.GetAll((from) =>
                    tableStorageService.Query<DataKeyLocationEntry>(t => t.Timestamp > from).ToList()))
                    .Select(t => t.Shallowcopy<DataKeyLocationEntry>()).ToList();
        }

        public async Task UpdateLocation(DataKeyLocationEntry[] entries)
        {
            DateTimeOffset from = DateTimeOffset.Now;
            await tableStorageService.PrepareUpsert(entries).ExecuteBatch();
            await CacheManager.Bust(typeof(DataKeyLocationEntry).Name, false, from);
            await CacheManager.UpsertCache(typeof(DataKeyLocationEntry).Name, entries);
        }

        public async Task<DataKeyLocationEntry[]> InsertLocation(DataKeyLocationEntry[] entries)
        {
            DateTimeOffset from = DateTimeOffset.Now;
            foreach (var entry in entries)
            {
                entry.PartitionKey = "All";
                entry.RowKey = Guid.NewGuid().ToString();
            }
            await tableStorageService.PrepareInsert(entries).ExecuteBatch();
            await CacheManager.Bust(typeof(DataKeyLocationEntry).Name, true, from);
            await CacheManager.UpsertCache(typeof(DataKeyLocationEntry).Name, entries);
            return entries;
        }
    }
}
