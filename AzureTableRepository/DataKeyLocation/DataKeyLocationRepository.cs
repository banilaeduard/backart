using AzureServices;
using Microsoft.Extensions.Logging;
using RepositoryContract.DataKeyLocation;

namespace AzureTableRepository.DataKeyLocation
{
    public class DataKeyLocationRepository : IDataKeyLocationRepository
    {
        TableStorageService tableStorageService;

        public DataKeyLocationRepository(ILogger<TableStorageService> logger)
        {
            tableStorageService = new TableStorageService(logger);
        }

        public async Task DeleteLocation(DataKeyLocationEntry[] entries)
        {
            await tableStorageService.PrepareDelete(entries).ExecuteBatch();
            CacheManager.Bust(typeof(DataKeyLocationEntry).Name, true, null);
            CacheManager.RemoveFromCache(typeof(DataKeyLocationEntry).Name, entries);
        }

        public async Task<IList<DataKeyLocationEntry>> GetLocations()
        {
            return CacheManager.GetAll((from) =>
                    tableStorageService.Query<DataKeyLocationEntry>(t => t.Timestamp > from).Select(t => t.Shallowcopy()).ToList()
                    ).ToList();
        }

        public async Task UpdateLocation(DataKeyLocationEntry[] entries)
        {
            DateTimeOffset from = DateTimeOffset.Now;
            await tableStorageService.PrepareUpsert(entries).ExecuteBatch();
            CacheManager.Bust(typeof(DataKeyLocationEntry).Name, false, from);
            CacheManager.UpsertCache(typeof(DataKeyLocationEntry).Name, entries);
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
            CacheManager.Bust(typeof(DataKeyLocationEntry).Name, true, from);
            CacheManager.UpsertCache(typeof(DataKeyLocationEntry).Name, entries);
            return entries;
        }
    }
}
