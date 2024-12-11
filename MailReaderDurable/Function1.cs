using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using AzureServices;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;

namespace MailReaderDurable
{
    public static class Function1
    {
        [FunctionName("MailFetchThrottle")]
        public static async Task<List<string>> MailFetchThrottle(
            [OrchestrationTrigger] IDurableOrchestrationContext context,
            ILogger log)
        {
            var logger = context.CreateReplaySafeLogger(log);
            CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
            CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;

            using (var timeoutCts = new CancellationTokenSource())
            {
                //var client = await QueueService.GetClient("movemailto");
                DateTime dueTime = context.CurrentUtcDateTime.AddMinutes(5);

                Task durableTimeout = context.CreateTimer(dueTime, timeoutCts.Token);
                Task<bool> approvalEvent = context.WaitForExternalEvent<bool>("ExecuteNow");

                await Task.WhenAny(approvalEvent, durableTimeout);
                try
                {
                    timeoutCts.Cancel();
                }
                catch (Exception ex) { }

                var client = await QueueService.GetClient("mailoperations");
                while (client.PeekMessage(CancellationToken.None).Value != null)
                {
                    var messages = await client.ReceiveMessagesAsync(maxMessages: 1);

                    foreach (var message in messages.Value.OrderBy(x => x.InsertedOn))
                    {
                        var body = message.Body.ToString()!;
                        context.SignalEntity(new EntityId(nameof(ProcessMails), "source1"), "add", body);
                        //foreach (var item in body.Items)
                        //    finalList[item] = body.DestinationFolder;
                    }
                }
                // Two-way call to the entity which returns a value - awaits the response
            }
            return [];
        }

        // AGGREGATOR PROXY
        [FunctionName(nameof(ProcessMails))]
        public static void ProcessMails([EntityTrigger] IDurableEntityContext ctx, ILogger log)
        {
            CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
            CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;
            ctx.Return(1);
            var size = ctx.BatchSize;
            var pos = ctx.BatchPosition;
            switch (ctx.OperationName.ToLowerInvariant())
            {
                case "add":
                    ctx.SetState(ctx.GetState<int>() + ctx.GetInput<int>());
                    break;
                case "reset":
                    ctx.SetState(0);
                    break;
                case "get":
                    ctx.Return(ctx.GetState<int>());
                    break;
            }
        }

        [FunctionName("MailFetchThrottle_HttpStart")]
        public static async Task<HttpResponseMessage> HttpStart(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestMessage req,
            [DurableClient] IDurableOrchestrationClient starter,
            ILogger log)
        {
            CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
            CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;
            // Function input comes from the request content.
            var orchest = await starter.ListInstancesAsync(new OrchestrationStatusQueryCondition()
            {
                InstanceIdPrefix = "source1",
                RuntimeStatus = [
                OrchestrationRuntimeStatus.Running,
                OrchestrationRuntimeStatus.Pending,
                OrchestrationRuntimeStatus.Suspended,
                ]
            }, CancellationToken.None);
            var instanceId = "source1";
            if (!orchest.DurableOrchestrationState.Any())
            {
                instanceId = await starter.StartNewAsync(nameof(MailFetchThrottle), instanceId);
                log.LogInformation("Started orchestration with ID = '{instanceId}'.", instanceId);
            }
            else
            {
                log.LogInformation("Already started orchestration with ID = '{instanceId}'.", instanceId);
            }

            return starter.CreateCheckStatusResponse(req, instanceId);
        }

        [FunctionName("RaiseEventToMailFetchThrottle")]
        public static async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequest req,
        [DurableClient] IDurableOrchestrationClient client)
        {
            CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
            CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;
            await client.RaiseEventAsync(req.Query["instanceId"], "ExecuteNow", true);
            return await client.WaitForCompletionOrCreateCheckStatusResponseAsync(req, req.Query["instanceId"], TimeSpan.FromSeconds(10));
        }
    }
}
