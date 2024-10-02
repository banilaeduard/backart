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

        public async Task DeleteLocation(DataKeyLocationEntry entry)
        {
            await tableStorageService.Delete(entry);
            CacheManager.Bust(typeof(DataKeyLocationEntry).Name, true, null);
            CacheManager.RemoveFromCache(typeof(DataKeyLocationEntry).Name, [entry]);
        }

        public async Task<IList<DataKeyLocationEntry>> GetLocations()
        {
            return CacheManager.GetAll((from) =>
                    tableStorageService.Query<DataKeyLocationEntry>(t => t.Timestamp > from).Select(t => t.Shallowcopy()).ToList()
                    ).ToList();
        }

        public async Task UpdateLocation(DataKeyLocationEntry entry)
        {
            DateTimeOffset from = DateTimeOffset.Now;
            await tableStorageService.Update(entry);
            CacheManager.Bust(typeof(DataKeyLocationEntry).Name, false, from);
            CacheManager.UpsertCache(typeof(DataKeyLocationEntry).Name, [entry]);
        }

        public async Task InsertLocation(DataKeyLocationEntry entry)
        {
            DateTimeOffset from = DateTimeOffset.Now;
            entry.PartitionKey = "All";
            entry.RowKey = Guid.NewGuid().ToString();
            await tableStorageService.Insert(entry);
            CacheManager.Bust(typeof(DataKeyLocationEntry).Name, true, from);
            CacheManager.UpsertCache(typeof(DataKeyLocationEntry).Name, [entry]);
        }
    }
}
