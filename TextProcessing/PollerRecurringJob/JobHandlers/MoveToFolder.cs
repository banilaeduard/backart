using AzureServices;
using EntityDto;
using RepositoryContract;
using YahooFeeder;

namespace PollerRecurringJob.JobHandlers
{
    internal static class MoveToFolder
    {
        internal static async Task Execute(PollerRecurringJob jobContext)
        {
            var client = await QueueService.GetClient("movemailto");
            var messages = await client.ReceiveMessagesAsync(maxMessages: 32);

            var finalList = new Dictionary<TableEntityPK, string>(TableEntityPK.GetComparer<TableEntityPK>());

            foreach (var message in messages.Value.OrderBy(x => x.InsertedOn))
            {
                var body = QueueService.Deserialize<MoveToMessage<TableEntityPK>>(message.Body.ToString())!;
                foreach (var item in body.Items)
                    finalList[item] = body.DestinationFolder;
            }

            if (finalList.Any())
            {
                var proxy = jobContext.serviceProxy.CreateServiceProxy<IYahooFeeder>(new Uri("fabric:/TextProcessing/YahooTFeederType"));
                await proxy.Move([..finalList.GroupBy(l => l.Value).Select(x =>
                    new MoveToMessage<TableEntityPK>() {
                            DestinationFolder = x.Key,
                            Items = x.Select(it => it.Key).Distinct()
                        })]);
            }

            foreach (var item in messages.Value)
            {
                await client.DeleteMessageAsync(item.MessageId, item.PopReceipt);
            }
        }
    }
}