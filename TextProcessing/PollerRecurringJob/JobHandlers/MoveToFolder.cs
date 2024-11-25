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

            var finalList = new List<MoveToMessage<TableEntityPK>>();
            var msgList = new List<(string messageId, string popReceipt)>();

            foreach (var message in messages.Value)
            {
                var body = QueueService.Deserialize<MoveToMessage<TableEntityPK>>(message.Body.ToString())!;
                finalList.Add(body);

                msgList.Add((message.MessageId, message.PopReceipt));
            }

            if (finalList.SelectMany(x => x.Items).Any())
            {
                var proxy = jobContext.serviceProxy.CreateServiceProxy<IYahooFeeder>(new Uri("fabric:/TextProcessing/YahooTFeederType"));
                await proxy.Move([..finalList.GroupBy(l => l.DestinationFolder).Select(x =>
                    new MoveToMessage<TableEntityPK>() {
                            DestinationFolder = x.Key,
                            Items = x.SelectMany(it => it.Items).Distinct()
                        })]);
                foreach (var item in msgList)
                {
                    await client.DeleteMessageAsync(item.messageId, item.popReceipt);
                }
            }
        }
    }
}