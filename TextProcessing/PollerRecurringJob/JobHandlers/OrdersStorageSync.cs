using Microsoft.Extensions.DependencyInjection;
using Microsoft.ServiceFabric.Services.Client;
using RepositoryContract.CommitedOrders;
using RepositoryContract.Imports;
using RepositoryContract.Orders;
using V2.Interfaces;

namespace PollerRecurringJob.JobHandlers
{
    internal static class OrdersStorageSync
    {
        internal static async Task Execute(PollerRecurringJob jobContext)
        {
            var ordersImportsRepository = jobContext.provider.GetRequiredService<IImportsRepository>()!;

            var commitedOrdersRepository = jobContext.provider.GetRequiredService<ICommitedOrdersRepository>()!;
            var ordersRepository = jobContext.provider.GetRequiredService<IOrdersRepository>()!;

            var lastCommited = await commitedOrdersRepository.GetLastSyncDate() ?? new DateTime(2024, 9, 1);
            var lastOrder = await ordersRepository.GetLastSyncDate() ?? new DateTime(2024, 5, 5);

            var latest = await ordersImportsRepository.PollForNewContent();

            if (latest.commited > lastCommited)
            {
                try
                {
                    await RunPublish(jobContext);
                }
                catch (Exception ex)
                {
                    ActorEventSource.Current.ActorMessage(jobContext, $@"Exception : {ex.Message}. {ex.StackTrace}");
                }
            }

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

        private static Task RunPublish(PollerRecurringJob jobContext)
        {
            return jobContext.serviceProxy.CreateServiceProxy<IWorkLoadService>(new Uri("fabric:/TextProcessing/WorkLoadService"), ServicePartitionKey.Singleton).ThrottlePublish(TimeSpan.FromSeconds(1));
        }
    }
}
