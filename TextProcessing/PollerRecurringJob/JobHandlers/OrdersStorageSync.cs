using AzureServices;
using AzureTableRepository.CommitedOrders;
using AzureTableRepository.Orders;
using SqlTableRepository.Orders;

namespace PollerRecurringJob.JobHandlers
{
    internal static class OrdersStorageSync
    {
        internal static async Task Execute(PollerRecurringJob jobContext)
        {
            var storage = new BlobAccessStorageService();
            var ordersImportsRepository = new OrdersImportsRepository(storage);
            var commitedOrdersRepository = new CommitedOrdersRepository(null);
            var ordersRepository = new OrdersRepository(null);

            var lastOrder = await jobContext.StateManager.GetOrAddStateAsync("order", DateTime.Now.AddDays(-7));
            var lastCommited = await jobContext.StateManager.GetOrAddStateAsync("commited", DateTime.Now.AddDays(-7));

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
            await jobContext.StateManager.AddOrUpdateStateAsync("order", latest.order, (_, _) => latest.order);
            await jobContext.StateManager.AddOrUpdateStateAsync("commited", latest.commited, (_, _) => latest.commited);
        }
    }
}
