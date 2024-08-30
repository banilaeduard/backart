using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Actors.Client;
using Microsoft.ServiceFabric.Actors.Runtime;
using System.Fabric;
using YahooFeederJob.Interfaces;

namespace YahooFeederJob
{
    internal class YahooFeederJobActorService<T> : ActorService where T : IActor
    {
        public YahooFeederJobActorService(StatefulServiceContext context, ActorTypeInformation typeInfo, Func<ActorService, ActorId, ActorBase> actorFactory)
        : base(context, typeInfo, actorFactory)
        { }

/*        protected override IEnumerable<ServiceReplicaListener> CreateServiceReplicaListeners()
        {
            var listeners = base.CreateServiceReplicaListeners();
            return new List<ServiceReplicaListener>(listeners)
                    {
                        new ServiceReplicaListener(c => new FabricTransportServiceRemotingListener(c, this)),
                    };
        }*/

        protected async override Task RunAsync(CancellationToken cancellationToken)
        {
            await base.RunAsync(cancellationToken);

            if (typeof(IYahooFeederJob).IsAssignableFrom(typeof(T)))
            {
                var actorServiceProxy = ActorProxy.Create<IYahooFeederJob>(new ActorId("yM"), new Uri("fabric:/TextProcessing/YahooFeederJobActorService"));
                await actorServiceProxy.SetOptions(new MailSettings()
                {
                    Folders = Environment.GetEnvironmentVariable("y_folders")!.Split(";", StringSplitOptions.TrimEntries),
                    From = Environment.GetEnvironmentVariable("y_from")!.Split(";", StringSplitOptions.TrimEntries),
                    DaysBefore = int.Parse(Environment.GetEnvironmentVariable("days_before")!),
                    Password = Environment.GetEnvironmentVariable("Password")!,
                    User = Environment.GetEnvironmentVariable("User")!
                });
            }
        }
    }
}