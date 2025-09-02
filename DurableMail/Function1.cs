using AzureServices;
using AzureTableRepository.CommitedOrders;
using AzureTableRepository.DataKeyLocation;
using AzureTableRepository.MailSettings;
using AzureTableRepository.Orders;
using AzureTableRepository.Tickets;
using EntityDto;
using MailReader.MailOperations;
using Microsoft.AspNetCore.Http;
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
using System.Globalization;

namespace DurableMail
{
    public static class Function1
    {
        [Function("FetchMailOrchestrator")]
        public static async Task RunOrchestrator(
            [OrchestrationTrigger] TaskOrchestrationContext context)
        {
            var logger = context.CreateReplaySafeLogger("FetchMailOrchestrator");
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

                var serviceProvider = BuildServiceProvider(logger);
                YahooTFeeder.logger = logger;
                List<Task> taskList = [];
                var cfg = await GetSettings(context.InstanceId, logger, serviceProvider);

                var client = await YahooTFeeder.ConnectAsync(cfg.mailSource, CancellationToken.None);

                try
                {
                    await GetMoveQueue(async (moveNew, downloadNew) =>
                    {
                        await YahooTFeeder.Batch(client, cfg.mailSource, cfg.mailSettingEntries,
                        serviceProvider,
                        Operation.Move | Operation.Fetch | Operation.Download,
                        [.. downloadNew],
                        [.. moveNew],
                    CancellationToken.None);
                    }, serviceProvider);

                    await GetMoveQueue(async (moveNew, downloadNew) =>
                    {
                        if (moveNew.Any() || downloadNew.Any())
                        {
                            await YahooTFeeder.Batch(client, cfg.mailSource, cfg.mailSettingEntries, serviceProvider,
                                Operation.Move | Operation.Download,
                                [.. downloadNew],
                                [.. moveNew],
                        CancellationToken.None);
                        }
                    }, serviceProvider);
                }
                catch (Exception ex)
                {
                    logger.LogError($@"{ex.Message} - {ex.StackTrace}");
                    throw;
                }
                finally
                {
                    if (client.IsValueCreated)
                    {
                        await client.Value.DisconnectAsync(true);
                        client.Value.Dispose();
                    }
                }
            }
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
            var tasks = client.GetAllInstancesAsync(new OrchestrationQuery()
            {
                InstanceIdPrefix = instanceId
            }).ToBlockingEnumerable().ToList();

            if (!tasks.Any(x => x.RuntimeStatus == OrchestrationRuntimeStatus.Running))
            {
                // Function input comes from the request content.
                await client.ScheduleNewOrchestrationInstanceAsync("FetchMailOrchestrator", cancellation: CancellationToken.None,
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
                await client.RaiseEventAsync(req.Query["instanceId"], "ExecuteNow", true);
                timeoutCts.CancelAfter(1000 * 30);
                return await client.WaitForInstanceStartAsync(req.Query["instanceId"], timeoutCts.Token);
            }
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