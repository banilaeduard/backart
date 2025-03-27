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
            return new ServiceCollection()
                    .AddScoped<IMetadataService, FabricMetadataService>()
                    .AddScoped<ICacheManager<TicketEntity>, AlwaysGetCacheManager<TicketEntity>>()
                    .AddScoped<ICacheManager<AttachmentEntry>, AlwaysGetCacheManager<AttachmentEntry>>()
                    .AddScoped<IMailSettingsRepository, MailSettingsRepository>()
                    .AddScoped<ITicketEntryRepository, TicketEntryRepository>()
                    .AddScoped<IWorkflowTrigger, QueueService>()
                    .AddScoped<TableStorageService, TableStorageService>()
                    .AddScoped<IStorageService, BlobAccessStorageService>()
                    .BuildServiceProvider();
        }
    }
}
