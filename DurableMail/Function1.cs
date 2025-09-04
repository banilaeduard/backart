using AzureServices;
using AzureTableRepository.CommitedOrders;
using AzureTableRepository.DataKeyLocation;
using AzureTableRepository.MailSettings;
using AzureTableRepository.Orders;
using AzureTableRepository.Tickets;
using EntityDto;
using MailKit.Net.Imap;
using MailReader.MailOperations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.DurableTask;
using Microsoft.DurableTask.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RepositoryContract;
using RepositoryContract.CommitedOrders;
using RepositoryContract.DataKeyLocation;
using RepositoryContract.MailSettings;
using RepositoryContract.Orders;
using RepositoryContract.Tickets;
using ServiceImplementation.Caching;
using ServiceInterface;
using ServiceInterface.Storage;

namespace DurableMail
{
    public static class Function1
    {
        [Function("ProcessMail")]
        public static async Task ProcessMail([ActivityTrigger] string instanceId, FunctionContext context)
        {
            var log = context.GetLogger("ProcessMail");
            log.LogInformation($"[{instanceId}] Starting mail processing...");

            Lazy<ImapClient> client = null;

            try
            {
                // Build services, configs, etc.
                var serviceProvider = BuildServiceProvider(log);
                YahooTFeeder.logger = log;
                var cfg = await GetSettings(instanceId, log, serviceProvider);

                client = await YahooTFeeder.ConnectAsync(cfg.mailSource, CancellationToken.None);

                log.LogInformation("Executing round 1...");
                await GetMoveQueue(async (moveNew, downloadNew) =>
                {
                    await YahooTFeeder.Batch(
                        client, cfg.mailSource, cfg.mailSettingEntries, serviceProvider,
                        Operation.Move | Operation.Fetch | Operation.Download,
                        [.. downloadNew],
                        [.. moveNew],
                        CancellationToken.None);
                }, serviceProvider);

                log.LogInformation("Executing round 2...");
                await GetMoveQueue(async (moveNew, downloadNew) =>
                {
                    if (moveNew.Any() || downloadNew.Any())
                    {
                        await YahooTFeeder.Batch(
                            client, cfg.mailSource, cfg.mailSettingEntries, serviceProvider,
                            Operation.Move | Operation.Download,
                            [.. downloadNew],
                            [.. moveNew],
                            CancellationToken.None);
                    }
                }, serviceProvider);

                log.LogInformation($"[{instanceId}] Mail processing finished.");
            }
            catch (Exception ex)
            {
                log.LogError(ex, $"[{instanceId}] Error during mail processing");
                throw; // important: let orchestration fail
            }
            finally
            {
                if (client != null && client.IsValueCreated)
                {
                    try
                    {
                        await client.Value.DisconnectAsync(true);
                        client.Value.Dispose();
                    }
                    catch { }
                }
            }
        }


        [Function("FetchMailOrchestrator")]
        public static async Task RunOrchestrator(
            [OrchestrationTrigger] TaskOrchestrationContext context)
        {
            //var logger = context.CreateReplaySafeLogger("FetchMailOrchestrator");

            // Wait for external event OR timeout
            //var cts = new CancellationTokenSource();
            //var externalEvent = context.WaitForExternalEvent<bool>("ExecuteNow");
            //var timeout = context.CreateTimer(context.CurrentUtcDateTime.AddMinutes(5), cts.Token);

            //context.SetCustomStatus("Waiting");
            //var winner = await Task.WhenAny(externalEvent, timeout);

            //if (winner == externalEvent)
            //{
            //    try
            //    {
            //        cts.Cancel();
            //    }
            //    catch { }
            //    timeout = null; // cancel timer
            //    context.SetCustomStatus("Triggered by event");
            //}
            //else
            //{
            //    context.SetCustomStatus("Triggered by timeout");
            //}

            context.SetCustomStatus("Started");
            // Call activity to process mail
            await context.CallActivityAsync("ProcessMail", context.InstanceId);

            context.SetCustomStatus("Completed");
            return;
        }

        internal static async Task GetMoveQueue(Func<List<MoveToMessage<TableEntityPK>>, List<TableEntityPK>, Task> handler, ServiceProvider provider)
        {
            IWorkflowTrigger client = provider.GetRequiredService<IWorkflowTrigger>()!;
            var items = await client.GetWork<MoveToMessage<TableEntityPK>>("movemailto");

            var finalList = new Dictionary<TableEntityPK, string>(TableEntityPK.GetComparer<TableEntityPK>());
            foreach (var message in items.OrderBy(t => t.Timestamp))
            {
                foreach (var item in message.Model.Items)
                    finalList[item] = message.Model.DestinationFolder;
            }

            List<MoveToMessage<TableEntityPK>> move = new();
            if (finalList.Any())
            {
                move = finalList.GroupBy(l => l.Value).Select(x =>
                    new MoveToMessage<TableEntityPK>()
                    {
                        DestinationFolder = x.Key,
                        Items = x.Select(it => it.Key).Distinct()
                    }).ToList();
            }
            var downloadLazy = move.Where(x => x.DestinationFolder == "_PENDING_").SelectMany(x => x.Items).Distinct().ToList();

            await handler(move, downloadLazy);
            if (items.Any())
            {
                await client.ClearWork("movemailto", [.. items]);
            }
        }


        internal static async Task<(MailSourceEntry mailSource, List<MailSettingEntry> mailSettingEntries)> GetSettings(string Source, 
            ILogger? logger, ServiceProvider provider)
        {
            IMailSettingsRepository mailSettings = provider.GetService<IMailSettingsRepository>()!;

            var settings = (await mailSettings.GetMailSource()).FirstOrDefault(t => t.PartitionKey == Source);

            if (settings == null)
            {
                logger?.LogError("No settings for {0}. Cannot run the mail service", Source);
                throw new ArgumentException("MAIL SOURCE");
            }
            var mSettings = (await mailSettings.GetMailSetting(settings.Source)).ToList();

            return (settings, mSettings);
        }

        [Function("FetchMail_HttpStart")]
        public static async Task<HttpResponseData> HttpStart(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestData req,
            [DurableClient] DurableTaskClient client,
            FunctionContext executionContext)
        {
            ILogger logger = executionContext.GetLogger("FetchMail_HttpStart");
            string instanceId = req.Query["instanceId"]!;
            bool forceClose = bool.TryParse(req.Query["forceClose"], out var tn) && tn;
            var tasks = client.GetAllInstancesAsync(new OrchestrationQuery()
            {
                InstanceIdPrefix = instanceId
            }).ToBlockingEnumerable().ToList();
            // Function input comes from the request content.

            if (tasks.Any(t => t.IsRunning))
            {
                if (forceClose)
                {
                    foreach (var task in tasks.Where(t => t.IsRunning))
                    {
                        logger.LogInformation("Terminating existing orchestration with ID = '{instanceId}'.", task.InstanceId);
                        await client.TerminateInstanceAsync(task.InstanceId, "Forced by new start request", CancellationToken.None);
                    }
                }
                else
                {
                    logger.LogInformation("Orchestration with ID = '{instanceId}' is already running.", instanceId);
                    return await client.CreateCheckStatusResponseAsync(req, instanceId);
                }
            }

            instanceId = await client.ScheduleNewOrchestrationInstanceAsync("FetchMailOrchestrator", cancellation: CancellationToken.None,
                options: new StartOrchestrationOptions()
                {
                    InstanceId = instanceId
                });

            logger.LogInformation("Started orchestration with ID = '{instanceId}'.", instanceId);

            // Returns an HTTP 202 response with an instance management payload.
            // See https://learn.microsoft.com/azure/azure-functions/durable/durable-functions-http-api#start-orchestration
            return await client.CreateCheckStatusResponseAsync(req, instanceId);
        }

        [Function("FetchOrchestrationList")]
        public static async Task<IActionResult> HttpOrchestrationList(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequestData req,
            [DurableClient] DurableTaskClient client,
            FunctionContext executionContext)
        {
            var runtimeStatuses = new[]
            {
                    OrchestrationRuntimeStatus.Running,
                    OrchestrationRuntimeStatus.Completed,
                    OrchestrationRuntimeStatus.Failed,
                    OrchestrationRuntimeStatus.Terminated
            };
            string instanceId = req.Query["instanceId"]!;
            var instances = client.GetAllInstancesAsync(new OrchestrationQuery()
            {
                InstanceIdPrefix = instanceId,
                CreatedFrom = DateTime.UtcNow.AddDays(-7),
                //Statuses = runtimeStatuses
            }).ToBlockingEnumerable().Select(i => new
            {
                    InstanceId = i.InstanceId,
                    Name = i.Name,
                    RuntimeStatus = i.RuntimeStatus.ToString(),
                    CreatedTime = i.CreatedAt,
                    LastUpdatedTime = i.LastUpdatedAt,
                    FailureDetails = i.FailureDetails?.ToString(),
            }).ToList();
            return new OkObjectResult(instances);
        }

        private static ServiceProvider BuildServiceProvider(ILogger logger)
        {
            return new ServiceCollection()
                    .AddSingleton((sp) => logger)
                    .AddScoped<IMetadataService, BlobAccessStorageService>()
                    .AddScoped<IMailSettingsRepository, MailSettingsRepository>()
                    .AddScoped<ICacheManager<OrderEntry>, AlwaysGetCacheManager<OrderEntry>>()
                    .AddScoped<ICacheManager<CommitedOrderEntry>, AlwaysGetCacheManager<CommitedOrderEntry>>()
                    .AddScoped<ICacheManager<DataKeyLocationEntry>, AlwaysGetCacheManager<DataKeyLocationEntry>>()
                    .AddScoped<ICommitedOrdersRepository, CommitedOrdersRepository>()
                    .AddScoped<IOrdersRepository, OrdersRepository>()
                    .AddScoped<IWorkflowTrigger, QueueService>()
                    .AddScoped<AzureFileStorage, AzureFileStorage>()
                    .AddScoped<IDataKeyLocationRepository, DataKeyLocationRepository>()
                    .AddScoped<IStorageService, BlobAccessStorageService>()
                    .AddScoped<ICacheManager<TicketEntity>, AlwaysGetCacheManager<TicketEntity>>()
                    .AddScoped<ICacheManager<AttachmentEntry>, AlwaysGetCacheManager<AttachmentEntry>>()
                    .AddScoped<ITicketEntryRepository, TicketEntryRepository>()
                    .AddScoped<TableStorageService, TableStorageService>()
                    .BuildServiceProvider();
        }
    }
}