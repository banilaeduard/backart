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

        protected async override Task RunAsync(CancellationToken cancellationToken)
        {
            await base.RunAsync(cancellationToken);
            var cfg = Context.CodePackageActivationContext.GetConfigurationPackageObject("Config");
            if (typeof(IYahooFeederJob).IsAssignableFrom(typeof(T)))
                await ActorProxy.Create<IYahooFeederJob>(new ActorId("yM"), "").SetOptions(new MailSettings()
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