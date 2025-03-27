using Microsoft.ServiceFabric.Actors.Client;
using Microsoft.ServiceFabric.Actors.Runtime;
using Microsoft.ServiceFabric.Actors;
using System.Fabric;
using PollerRecurringJob.Interfaces;

namespace PollerRecurringJob
{
    internal class ScheduledActorService<T> : ActorService where T : IActor
    {
        public ScheduledActorService(StatefulServiceContext context, ActorTypeInformation actorType, Func<ActorService, ActorId, ActorBase> factory) : base(context, actorType, factory)
        {
        }

        protected async override Task RunAsync(CancellationToken cancellationToken)
        {
            await base.RunAsync(cancellationToken);

            var proxy = ActorProxy.Create<IPollerRecurringJob>(new ActorId("poller"), new Uri("fabric:/TextProcessing/PollerRecurringJobActorService"));

            await proxy.Sync();
        }
    }
}
