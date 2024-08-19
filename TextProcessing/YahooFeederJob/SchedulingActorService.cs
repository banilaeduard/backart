using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Actors.Client;
using Microsoft.ServiceFabric.Actors.Runtime;
using System.Fabric;
using YahooFeederJob.Interfaces;

namespace YahooFeederJob
{
    internal class SchedulingActorService<T> : ActorService where T: IActor
    {
        public SchedulingActorService(StatefulServiceContext context, ActorTypeInformation typeInfo, Func<ActorService, ActorId, ActorBase> actorFactory)
        : base(context, typeInfo, actorFactory)
        { }

        protected async override Task RunAsync(CancellationToken cancellationToken)
        {
            await base.RunAsync(cancellationToken);
            var t = typeof(T);

            if (typeof(IYahooFeederJob).IsAssignableFrom(typeof(T)))
                await ActorProxy.Create<IYahooFeederJob>(new ActorId("yM"), "").SetOptions(new MailSettings()
                {
                    Folders = Environment.GetEnvironmentVariable("y_folders")!.Split(";", StringSplitOptions.TrimEntries),
                    From = Environment.GetEnvironmentVariable("y_from")!.Split(";", StringSplitOptions.TrimEntries)
                });
        }
    }
}