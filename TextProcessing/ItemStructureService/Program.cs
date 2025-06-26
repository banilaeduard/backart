using System.Diagnostics;
using AzureFabricServices;
using AzureTableRepository.ProductCodes;
using AzureTableRepository.Report;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.ServiceFabric.Services.Runtime;
using RepositoryContract.ProductCodes;
using RepositoryContract.Report;
using RepositoryServices;
using ServiceImplementation.Caching;
using ServiceInterface.Storage;
using ServiceInterface;
using Microsoft.Diagnostics.EventFlow.ServiceFabric;
using System.Globalization;

namespace ItemStructureService
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
                // The ServiceManifest.XML file defines one or more service type names.
                // Registering a service maps a service type name to a .NET type.
                // When Service Fabric creates an instance of this service type,
                // an instance of the class is created in this host process.

                ServiceRuntime.RegisterServiceAsync("ItemStructureServiceType",
                    context => new ItemStructureService(context, BuildServiceProvider())).GetAwaiter().GetResult();

                ServiceEventSource.Current.ServiceTypeRegistered(Process.GetCurrentProcess().Id, typeof(ItemStructureService).Name);

                // Prevents this host process from terminating so services keep running.
                Thread.Sleep(Timeout.Infinite);
            }
            catch (Exception e)
            {
                ServiceEventSource.Current.ServiceHostInitializationFailed(e.ToString());
                throw;
            }

            try
            {
                // The ServiceManifest.XML file defines one or more service type names.
                // Registering a service maps a service type name to a .NET type.
                // When Service Fabric creates an instance of this service type,
                // an instance of the class is created in this host process.
#if RELEASE
                using (var diagnosticsPipeline = ServiceFabricDiagnosticPipelineFactory.CreatePipeline("MyCompany-TextProcessing-WorkLoadService"))
                {
#endif
                    ServiceRuntime.RegisterServiceAsync("ItemStructureServiceType",
                    context => new ItemStructureService(context, BuildServiceProvider())).GetAwaiter().GetResult();

                    ServiceEventSource.Current.ServiceTypeRegistered(Process.GetCurrentProcess().Id, typeof(ItemStructureService).Name);
                    CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
                    CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;
                    // Prevents this host process from terminating so services keep running.
                    Thread.Sleep(Timeout.Infinite);
#if RELEASE
                }
#endif
            }
            catch (Exception e)
            {
                ServiceEventSource.Current.ServiceHostInitializationFailed(e.ToString());
                throw;
            }
        }

        private static ServiceProvider BuildServiceProvider()
        {
            return new ServiceCollection()
                    .AddSingleton<IMetadataService, FabricMetadataService>()
                    .AddTransient<StructuraReportWriter, StructuraReportWriter>()
#if RELEASE
                    .AddScoped<IProductCodeRepository, ProductCodesRepository>()
                    .AddScoped<IReportEntryRepository, ReportEntryRepository>()
#else
                    .AddScoped<IProductCodeRepository, ProductCodesRepository>()
                    .AddScoped<IReportEntryRepository, ReportEntryRepository>()
#endif
                    .AddSingleton<ICacheManager<ProductCodeEntry>, AlwaysGetCacheManager<ProductCodeEntry>>()
                    .AddSingleton<ICacheManager<ProductStatsEntry>, AlwaysGetCacheManager<ProductStatsEntry>>()
                    .AddSingleton<ICacheManager<ProductCodeStatsEntry>, AlwaysGetCacheManager<ProductCodeStatsEntry>>()
                    .BuildServiceProvider();
        }
    }
}