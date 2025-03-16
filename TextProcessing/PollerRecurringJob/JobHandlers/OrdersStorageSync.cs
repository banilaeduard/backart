using AzureFabricServices;
using AzureServices;
using AzureTableRepository;
using AzureTableRepository.CommitedOrders;
using AzureTableRepository.Orders;
using RepositoryContract.CommitedOrders;
using RepositoryContract.Orders;
using SqlTableRepository.Orders;

namespace PollerRecurringJob.JobHandlers
{
    internal static class OrdersStorageSync
    {
        internal static async Task Execute(PollerRecurringJob jobContext)
        {
            var storage = new BlobAccessStorageService();
            var ordersImportsRepository = new OrdersImportsRepository(storage);

            var metadataService = new FabricMetadataService();
            var commitedOrdersRepository = new CommitedOrdersRepository(new CacheManager<CommitedOrderEntry>(metadataService), metadataService);
            var ordersRepository = new OrdersRepository(new CacheManager<OrderEntry>(metadataService), metadataService);

            var lastCommited = await commitedOrdersRepository.GetLastSyncDate() ?? new DateTime(2024, 9, 1);
            var lastOrder = await ordersRepository.GetLastSyncDate() ?? new DateTime(2024, 5, 5);

            var latest = await ordersImportsRepository.PollForNewContent();

            if (latest.order > lastOrder && latest.commited > lastCommited)
            {
                ActorEventSource.Current.ActorMessage(jobContext, $"Polling Actor. Fetching orders {latest.order} and commited {latest.commited}");
                var sourceOrders = await ordersImportsRepository.GetImportCommitedOrders(lastCommited, new DateTime(2024, 5, 5));
                await commitedOrdersRepository.ImportCommitedOrders(sourceOrders.commited, latest.commited);
                await ordersRepository.ImportOrders(sourceOrders.orders, latest.order);
                ActorEventSource.Current.ActorMessage(jobContext, $"Polling Actor. Fetched orders {sourceOrders.orders.Count} and commited {sourceOrders.commited.Count}");
            }
            else if (latest.order > lastOrder)
            {
                ActorEventSource.Current.ActorMessage(jobContext, $"Polling Actor. Fetching orders {latest.order}");
                var sourceOrders = await ordersImportsRepository.GetImportOrders(new DateTime(2024, 5, 5));
                await ordersRepository.ImportOrders(sourceOrders, latest.order);
                ActorEventSource.Current.ActorMessage(jobContext, $"Polling Actor. Fetched orders {sourceOrders.Count}");
            }
            else if (latest.commited > lastCommited)
            {
                ActorEventSource.Current.ActorMessage(jobContext, $"Polling Actor. Fetching commited {latest.commited}");
                var commited = await ordersImportsRepository.GetImportCommited(lastCommited);
                await commitedOrdersRepository.ImportCommitedOrders(commited, latest.commited);
                ActorEventSource.Current.ActorMessage(jobContext, $"Polling Actor. Fetched commited {commited.Count}");
            }
        }
    }
}
