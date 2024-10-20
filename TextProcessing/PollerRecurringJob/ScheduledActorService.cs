using Microsoft.ServiceFabric.Actors.Client;
using Microsoft.ServiceFabric.Actors.Runtime;
using Microsoft.ServiceFabric.Actors;
using System.Fabric;
using PollerRecurringJob.Interfaces;

namespace PollerRecurringJob
{
    internal class ScheduledActorService<T> : ActorService where T : IActor
    {
        private ActorServiceProxy actorServiceProxy;

        public ScheduledActorService(StatefulServiceContext context, ActorTypeInformation actorType) : base(context, actorType)
        {
            actorServiceProxy = new ActorServiceProxy();
        }

        protected async override Task RunAsync(CancellationToken cancellationToken)
        {
            await base.RunAsync(cancellationToken);

            //var proxy = ActorProxy.Create<IPollerRecurringJob>(new ActorId(0), new Uri("fabric:/TextProcessing/PollerRecurringJobActorService"));

            //await proxy.Sync();
        }
    }
}