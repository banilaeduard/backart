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
            CacheManager<DataKeyLocationEntry>.Bust();
        }

        public async Task<IList<DataKeyLocationEntry>> GetLocations()
        {
            return CacheManager<DataKeyLocationEntry>.GetAll(() =>
                    tableStorageService.Query<DataKeyLocationEntry>(t => true).Select(t => t.Shallowcopy()).ToList()
                    ).ToList();
        }

        public async Task UpdateLocation(DataKeyLocationEntry entry)
        {
            await tableStorageService.Update(entry);
            CacheManager<DataKeyLocationEntry>.Bust();
        }

        public async Task InsertLocation(DataKeyLocationEntry entry)
        {
            entry.PartitionKey = "All";
            entry.RowKey = Guid.NewGuid().ToString();
            await tableStorageService.Insert(entry);
            CacheManager<DataKeyLocationEntry>.Bust();
        }
    }
}
