using EntityDto;
using MailReader.Interfaces;
using Microsoft.ServiceFabric.Actors.Client;
using Microsoft.ServiceFabric.Actors;
using RepositoryContract;
using ServiceInterface.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace PollerRecurringJob.JobHandlers
{
    internal static class MoveToFolder
    {
        internal static async Task Execute(PollerRecurringJob jobContext)
        {
            IWorkflowTrigger client = jobContext.provider.GetRequiredService<IWorkflowTrigger>()!;
            var items = await client.GetWork<MoveToMessage<TableEntityPK>>("movemailto");

            var finalList = new Dictionary<TableEntityPK, string>(TableEntityPK.GetComparer<TableEntityPK>());

            foreach (var message in items.OrderBy(t => t.Timestamp))
            {
                foreach (var item in message.Model.Items)
                    finalList[item] = message.Model.DestinationFolder;
            }

            if (finalList.Any())
            {
                var proxy = ActorProxy.Create<IMailReader>(new ActorId("source1"), new Uri("fabric:/TextProcessing/MailReaderActorService"));

                var request = finalList.GroupBy(l => l.Value).Select(x =>
                    new MoveToMessage<TableEntityPK>()
                    {
                        DestinationFolder = x.Key,
                        Items = x.Select(it => it.Key).Distinct()
                    }).ToList();
                await proxy.BatchAsync(request);
            }

            await client.ClearWork("movemailto", [.. items]);
        }
    }
}
