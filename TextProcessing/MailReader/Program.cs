using System;
using System.Diagnostics;
using System.Fabric;
using System.Globalization;
using AzureFabricServices;
using AzureServices;
using Microsoft.Diagnostics.EventFlow.ServiceFabric;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.ServiceFabric.Actors.Runtime;
using ServiceImplementation.Caching;
using ServiceInterface.Storage;
using ServiceInterface;
using RepositoryContract.MailSettings;
using AzureTableRepository.MailSettings;
using RepositoryContract.Tickets;
using AzureTableRepository.Tickets;
using AzureTableRepository.CommitedOrders;
using AzureTableRepository.DataKeyLocation;
using AzureTableRepository.Orders;
using Dapper;
using RepositoryContract.CommitedOrders;
using RepositoryContract.DataKeyLocation;
using RepositoryContract.ExternalReferenceGroup;
using RepositoryContract.Imports;
using RepositoryContract.Orders;
using RepositoryContract.Tasks;
using RepositoryContract.Transports;
using SqlTableRepository.ExternalReferenceGroup;
using SqlTableRepository.Orders;
using SqlTableRepository.Tasks;
using SqlTableRepository.Transport;
using System.Data;

namespace MailReader
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
                using (var diagnosticsPipeline = ServiceFabricDiagnosticPipelineFactory.CreatePipeline("MyCompany-TextProcessing-MailReader"))
                {
#endif
                ActorRuntime.RegisterActorAsync<MailReader>((context, actorType) => new ActorService(context, actorType, (svc, actorId) => new MailReader(svc, actorId, BuildServiceProvider()))).GetAwaiter().GetResult();
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
            SqlMapper.AddTypeHandler(new DateTimeHandler());
            return new ServiceCollection()
                    .AddScoped<IMetadataService, FabricMetadataService>()
                    .AddScoped<IMailSettingsRepository, MailSettingsRepository>()
                    .AddScoped<ICacheManager<OrderEntry>, AlwaysGetCacheManager<OrderEntry>>()
                    .AddScoped<ICacheManager<CommitedOrderEntry>, AlwaysGetCacheManager<CommitedOrderEntry>>()
                    .AddScoped<ICacheManager<DataKeyLocationEntry>, AlwaysGetCacheManager<DataKeyLocationEntry>>()
                    .AddScoped<ICommitedOrdersRepository, CommitedOrdersRepository>()
                    .AddScoped<IOrdersRepository, OrdersRepository>()
                    .AddScoped<IWorkflowTrigger, QueueService>()
                    .AddScoped<ITaskRepository, TaskRepository>()
                    .AddScoped<AzureFileStorage, AzureFileStorage>()
                    .AddScoped<IDataKeyLocationRepository, DataKeyLocationRepository>()
                    .AddScoped<IImportsRepository, OrdersImportsRepository<AzureFileStorage>>()
                    .AddScoped<IExternalReferenceGroupRepository, ExternalReferenceGroupSql>()
                    .AddScoped<IStorageService, BlobAccessStorageService>()
                    .AddScoped<ICacheManager<TicketEntity>, AlwaysGetCacheManager<TicketEntity>>()
                    .AddScoped<ICacheManager<AttachmentEntry>, AlwaysGetCacheManager<AttachmentEntry>>()
                    .AddScoped<ITicketEntryRepository, TicketEntryRepository>()
                    .AddScoped<ITransportRepository, TransportRepository>()
                    .AddScoped<TableStorageService, TableStorageService>()
                    .BuildServiceProvider();
        }

        private class DateTimeHandler : SqlMapper.TypeHandler<DateTime>
        {
            public override void SetValue(IDbDataParameter parameter, DateTime value)
            {
                parameter.Value = value;
            }

            public override DateTime Parse(object value)
            {
                var v = (DateTime)value;
                return v.Kind == DateTimeKind.Unspecified ? DateTime.SpecifyKind((DateTime)value, DateTimeKind.Utc) : v;
            }
        }
    }
}
