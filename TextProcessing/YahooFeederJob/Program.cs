using DataAccess;
using DataAccess.Context;
using Microsoft.ServiceFabric.Actors.Runtime;

namespace YahooFeederJob
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

                /*                ActorRuntime.RegisterActorAsync<YahooFeederJob> (
                                   (context, actorType) => new ActorService(context, actorType)).GetAwaiter().GetResult();
                */
                var complaints = DbContextFactory.GetContext<ComplaintSeriesDbContext>(Environment.GetEnvironmentVariable("ConnectionString"), new NoFilterBaseContext());
                var jobStatus = DbContextFactory.GetContext<JobStatusContext>(Environment.GetEnvironmentVariable("ConnectionString"), new NoFilterBaseContext());

                ActorRuntime.RegisterActorAsync<YahooFeederJob>(
                                   (context, actorType) => new SchedulingActorService<YahooFeederJob>(context, actorType, (a, i) =>
                                   new YahooFeederJob(a, i, complaints, jobStatus))).GetAwaiter().GetResult();

                Thread.Sleep(Timeout.Infinite);
            }
            catch (Exception e)
            {
                ActorEventSource.Current.ActorHostInitializationFailed(e.ToString());
                throw;
            }
        }
    }
}
