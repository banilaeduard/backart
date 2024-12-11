using AzureServices;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.DurableTask;
using Microsoft.DurableTask.Client;
using Microsoft.DurableTask.Entities;
using Microsoft.Extensions.Logging;
using System.Globalization;

namespace DurableMail
{
    public static class Function1
    {
        [Function(nameof(RunOrchestrator))]
        public static async Task RunOrchestrator(
            [OrchestrationTrigger] TaskOrchestrationContext context)
        {
            var logger = context.CreateReplaySafeLogger(nameof(Function1));
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
                List<Task> taskList = [];
                var client = await QueueService.GetClient("mailoperations");
                while (client.PeekMessage(CancellationToken.None).Value != null)
                {
                    var messages = await client.ReceiveMessagesAsync(maxMessages: 1);

                    foreach (var message in messages.Value.OrderBy(x => x.InsertedOn))
                    {
                        var body = message.Body.ToString()!;
                        var entityId = new EntityInstanceId(nameof(ProcessMailsAsync), "source1_entity");
                        context.Entities.SignalEntityAsync(entityId, "OP_NAME", body);
                        //foreach (var item in body.Items)
                        //    finalList[item] = body.DestinationFolder;
                    }
                }
                // Two-way call to the entity which returns a value - awaits the response
            }
        }

        [Function(nameof(ProcessMailsAsync))]
        public static Task ProcessMailsAsync([EntityTrigger] TaskEntityDispatcher dispatcher)
        {
            CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
            CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;

            return dispatcher.DispatchAsync(operation =>
            {
                if (operation.State.GetState(typeof(int)) is null)
                {
                    operation.State.SetState(0);
                }

                switch (operation.Name.ToLowerInvariant())
                {
                    case "add":
                        int state = operation.State.GetState<int>();
                        state += operation.GetInput<int>();
                        operation.State.SetState(state);
                        return new(state);
                    case "reset":
                        operation.State.SetState(0);
                        break;
                    case "get":
                        return new(operation.State.GetState<int>());
                    case "delete":
                        operation.State.SetState(null);
                        break;
                }

                return default;
            });
        }

        [Function("Function1_HttpStart")]
        public static async Task<HttpResponseData> HttpStart(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestData req,
            [DurableClient] DurableTaskClient client,
            FunctionContext executionContext)
        {
            ILogger logger = executionContext.GetLogger("Function1_HttpStart");
            string instanceId = "source1";
            var tasks = client.GetAllInstancesAsync(new OrchestrationQuery()
            {
                InstanceIdPrefix = instanceId
            }).ToBlockingEnumerable().ToList();

            if (!tasks.Any(x => x.RuntimeStatus == OrchestrationRuntimeStatus.Running))
            {
                // Function input comes from the request content.
                await client.ScheduleNewOrchestrationInstanceAsync(nameof(RunOrchestrator), cancellation: CancellationToken.None,
                    options: new StartOrchestrationOptions()
                    {
                        InstanceId = instanceId
                    });
            }

            logger.LogInformation("Started orchestration with ID = '{instanceId}'.", instanceId);

            // Returns an HTTP 202 response with an instance management payload.
            // See https://learn.microsoft.com/azure/azure-functions/durable/durable-functions-http-api#start-orchestration
            return await client.CreateCheckStatusResponseAsync(req, instanceId);
        }

        [Function("RaiseEventToMailFetchThrottle")]
        public static async Task<OrchestrationMetadata> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequest req,
        [DurableClient] DurableTaskClient client)
        {
            using (var timeoutCts = new CancellationTokenSource())
            {
                CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
                CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;
                await client.RaiseEventAsync(req.Query["instanceId"], "ExecuteNow", true);
                timeoutCts.CancelAfter(1000 * 30);
                return await client.WaitForInstanceCompletionAsync(req.Query["instanceId"], timeoutCts.Token);
            }
        }
    }
}
