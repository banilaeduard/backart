using AzureFabricServices;
using AzureServices;
using AzureTableRepository.CommitedOrders;
using AzureTableRepository.Orders;
using Microsoft.Diagnostics.EventFlow.ServiceFabric;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.ServiceFabric.Actors.Runtime;
using RepositoryContract.CommitedOrders;
using RepositoryContract.Imports;
using RepositoryContract.Orders;
using RepositoryContract.Tasks;
using ServiceImplementation.Caching;
using ServiceInterface;
using ServiceInterface.Storage;
using SqlTableRepository.Orders;
using SqlTableRepository.Tasks;
using System.Globalization;
using System.Threading;

namespace PollerRecurringJob
{
    internal static class Program
    {
        /// <summary>
        /// This is the entry point of the service host process.
        /// </summary>
        private static void Main()
        {
            try
            {
                // This line registers an Actor Service to host your actor class with the Service Fabric runtime.
                // The contents of your ServiceManifest.xml and ApplicationManifest.xml files
                // are automatically populated when you build this project.
                // For more information, see https://aka.ms/servicefabricactorsplatform

#if RELEASE
                using (var diagnosticsPipeline = ServiceFabricDiagnosticPipelineFactory.CreatePipeline("MyCompany-TextProcessing-PollerRecurringJob"))
                {
#endif
                    ActorRuntime.RegisterActorAsync<PollerRecurringJob>(
                   (context, actorType) => new ScheduledActorService<PollerRecurringJob>(context, actorType, (svc, actorId) => new PollerRecurringJob(svc, actorId, BuildServiceProvider()))).GetAwaiter().GetResult();
                    CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
                    CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;
                    Thread.Sleep(Timeout.Infinite);
#if RELEASE
                }
#endif
            }
            catch (Exception e)
            {
                ActorEventSource.Current.ActorHostInitializationFailed(e.ToString());
                throw;
            }
        }

        private static ServiceProvider BuildServiceProvider()
        {
            return new ServiceCollection()
                    .AddScoped<IMetadataService, FabricMetadataService>()
                    .AddScoped<ICacheManager<OrderEntry>, AlwaysGetCacheManager<OrderEntry>>()
                    .AddScoped<ICacheManager<CommitedOrderEntry>, AlwaysGetCacheManager<CommitedOrderEntry>>()
                    .AddScoped<ICommitedOrdersRepository, CommitedOrdersRepository>()
                    .AddScoped<IOrdersRepository, OrdersRepository>()
                    .AddScoped<IWorkflowTrigger, QueueService>()
                    .AddScoped<ITaskRepository, TaskRepository>()
                    .AddScoped<AzureFileStorage, AzureFileStorage>()
                    .AddScoped<IImportsRepository, OrdersImportsRepository<AzureFileStorage>>()
                    .BuildServiceProvider();
        }
    }
}
