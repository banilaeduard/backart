using System.Diagnostics;
using System.Globalization;
using Microsoft.Diagnostics.EventFlow.ServiceFabric;
using Microsoft.ServiceFabric.Services.Runtime;

namespace MailExtrasExtractor
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
#if RELEASE
                using (var diagnosticsPipeline = ServiceFabricDiagnosticPipelineFactory.CreatePipeline("MyCompany-TextProcessing-MailExtrasExtractor-DiagnosticsPipeline"))
                {
#endif
                    ServiceRuntime.RegisterServiceAsync("MailExtrasExtractorType",
                    context => new MailExtrasExtractor(context)).GetAwaiter().GetResult();
                    CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
                    CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;
                    ServiceEventSource.Current.ServiceTypeRegistered(Process.GetCurrentProcess().Id, typeof(MailExtrasExtractor).Name);

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
    }
}
