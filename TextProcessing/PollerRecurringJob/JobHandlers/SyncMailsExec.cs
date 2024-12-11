using Microsoft.ServiceFabric.Actors.Client;
using Microsoft.ServiceFabric.Actors;
using MailReader.Interfaces;

namespace PollerRecurringJob.JobHandlers
{
    internal static class SyncMailsExec
    {
        internal static async Task Execute(PollerRecurringJob jobContext)
        {
            var proxy = ActorProxy.Create<IMailReader>(new ActorId("source1"), new Uri("fabric:/TextProcessing/MailReaderActorService"));
            await proxy.FetchMails();
        }
    }
}
