using AzureFabricServices;
using AzureServices;
using AzureTableRepository.CommitedOrders;
using AzureTableRepository.Orders;
using RepositoryContract.CommitedOrders;
using RepositoryContract.Orders;
using ServiceImplementation.Caching;
using SqlTableRepository.Orders;

namespace PollerRecurringJob.JobHandlers
{
    internal static class OrdersStorageSync
    {
        internal static async Task Execute(PollerRecurringJob jobContext)
        {
            var storage = new AzureFileStorage();
            var ordersImportsRepository = new OrdersImportsRepository<AzureFileStorage>(storage);

            var metadataService = new FabricMetadataService();
            var commitedOrdersRepository = new CommitedOrdersRepository(new AlwaysGetCacheManager<CommitedOrderEntry>(metadataService), metadataService);
            var ordersRepository = new OrdersRepository(new AlwaysGetCacheManager<OrderEntry>(metadataService), metadataService);

            var lastCommited = await commitedOrdersRepository.GetLastSyncDate() ?? new DateTime(2024, 9, 1);
            var lastOrder = await ordersRepository.GetLastSyncDate() ?? new DateTime(2024, 5, 5);

            var latest = await ordersImportsRepository.PollForNewContent();

            if (latest.order > lastOrder && latest.commited > lastCommited)
            {
                var sourceOrders = await ordersImportsRepository.GetImportCommitedOrders(lastCommited, new DateTime(2024, 5, 5));
                await commitedOrdersRepository.ImportCommitedOrders(sourceOrders.commited, latest.commited);
                await ordersRepository.ImportOrders(sourceOrders.orders, latest.order);
            }
            else if (latest.order > lastOrder)
            {
                var sourceOrders = await ordersImportsRepository.GetImportOrders(new DateTime(2024, 5, 5));
                await ordersRepository.ImportOrders(sourceOrders, latest.order);
            }
            else if (latest.commited > lastCommited)
            {
                var commited = await ordersImportsRepository.GetImportCommited(lastCommited);
                await commitedOrdersRepository.ImportCommitedOrders(commited, latest.commited);
            }
        }
    }
}
