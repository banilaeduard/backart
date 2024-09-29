using AzureServices;
using Microsoft.Extensions.Logging;
using RepositoryContract.CommitedOrders;

namespace AzureTableRepository.CommitedOrders
{
    public class CommitedOrdersRepository : ICommitedOrdersRepository
    {
        TableStorageService tableStorageService;
        public CommitedOrdersRepository(ILogger<TableStorageService> logger)
        {
            tableStorageService = new TableStorageService(logger);
        }

        public async Task DeleteCommitedOrders(List<DispozitieLivrareEntry> items)
        {
            await tableStorageService.PrepareDelete(items.ToList()).ExecuteBatch();
        }

        public async Task<List<DispozitieLivrareEntry>> GetCommitedOrders(Func<DispozitieLivrareEntry, bool> expr)
        {
            return tableStorageService.Query<DispozitieLivrareEntry>(t => true).ToList();
        }

        public async Task<List<DispozitieLivrareEntry>> GetCommitedOrders()
        {
            return CacheManager<DispozitieLivrareEntry>.GetAll(() => tableStorageService.Query<DispozitieLivrareEntry>(t => true).ToList()).ToList();
        }

        public async Task InsertCommitedOrder(DispozitieLivrareEntry sample)
        {
            await tableStorageService.Insert(sample);
            CacheManager<DispozitieLivrareEntry>.Bust();
        }
    }
}
