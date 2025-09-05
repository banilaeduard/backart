using EntityDto;
using EntityDto.Tasks;
using RepositoryContract;
using RepositoryContract.DataKeyLocation;
using RepositoryContract.Tasks;
using RepositoryContract.Tickets;
using ServiceInterface.Storage;

namespace PollerRecurringJob.MailOperations
{
    public class AddNewMailToExistingTasks : BackgroundService
    {
        private EventGridListener<List<AddMailToTask>> _listener = null;
        private ITaskRepository repo;
        private ILogger logger;
        private IDataKeyLocationRepository locationRepository;

        public AddNewMailToExistingTasks(ILogger<AddNewMailToExistingTasks> _logger, ITaskRepository _repo, IDataKeyLocationRepository _locationRepository)
        {
            repo = _repo;
            logger = _logger;
            locationRepository = _locationRepository;
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            if (_listener != null)
            {
                await _listener.DisposeAsync();
                _listener = null;
            }
            await base.StopAsync(cancellationToken);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            logger.LogInformation("Event Grid listener started.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await StartListening(stoppingToken);
                    await Task.Delay(Timeout.Infinite, stoppingToken);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Background listener crashed.");
                    await StopListening();
                    await Task.Delay(TimeSpan.FromSeconds(60), stoppingToken);
                }
            }
        }

        internal async Task StartListening(CancellationToken stoppingToken)
        {
            if (_listener == null)
            {
                logger.LogInformation("Start listening to eventgrid actor");
                _listener = new EventGridListener<List<AddMailToTask>>(
                        Environment.GetEnvironmentVariable("service_bus_conn")!,
                        "posting",
                        Process,
                        stoppingToken,
                        logger
                    );
                await _listener.StartAsync();
            }
        }

        internal async Task StopListening()
        {
            if (_listener != null)
            {
                logger.LogInformation("Stopped listening to eventgrid actor");
                var localList = _listener;
                _listener = null;
                await localList.DisposeAsync();
            }
        }

        private async Task Process(List<AddMailToTask> messages, CancellationToken token)
        {
            logger.LogInformation(@$"Processing eventgrid messages: {messages.Count}");
            if (!messages.Any()) return;

            var tasks = await repo.GetTasks(TaskInternalState.Open);
            var externalRefs = tasks.SelectMany(x => x.ExternalReferenceEntries.Where(t => t.EntityType == nameof(TicketEntity))).ToList().OrderBy(t => t.TaskId);

            var items = messages ?? [];
            if (!items.Any()) return;

            var items2 = items.Where(newMail => externalRefs.Any(er => er.ExternalGroupId.Equals(newMail.ThreadId))).ToList();

            // update only active tasks
            foreach (var task in tasks)
            {
                if (token.IsCancellationRequested) return;
                // make sure we don't have the external mail attached
                var intersect = items2.Where(newMail => externalRefs.Any(er => er.TaskId == task.Id
                        && er.ExternalGroupId.Equals(newMail.ThreadId)
                        && $"{er.PartitionKey}_{er.RowKey}_{er.EntityType}" != $"{newMail.PartitionKey}_{newMail.RowKey}_{newMail.EntityType}"
                    )).ToList();
                if (intersect.Any())
                {
                    task.ExternalReferenceEntries.AddRange(intersect.Select(ticket =>
                    new ExternalReferenceEntry()
                    {
                        PartitionKey = ticket.PartitionKey,
                        RowKey = ticket.RowKey,
                        TableName = ticket.TableName,
                        EntityType = ticket.EntityType,
                        Date = ticket.Date,
                        Action = ActionType.External,
                        Accepted = false,
                        ExternalGroupId = ticket.ThreadId
                    }));

                    await repo.UpdateTask(task);
                }
            }


            var newMails = items.Where(newMail => !externalRefs.Any(er => er.ExternalGroupId.Equals(newMail.ThreadId))).GroupBy(newMail => newMail.ThreadId).ToList() ?? [];
            if (newMails.Count > 0)
            {
                var mainLocs = (await locationRepository.GetLocations()).Where(loc => loc.MainLocation).ToList();

                foreach (var newMail in newMails)
                {
                    if (token.IsCancellationRequested) return;
                    var sample = newMail.First();
                    var hasMain = mainLocs.FirstOrDefault(l => l.PartitionKey == sample.LocationPartitionKey && l.RowKey == sample.LocationRowKey);

                    if (hasMain != null)
                    {
                        try
                        {
                            var task = await repo.SaveTask(new TaskEntry()
                            {
                                Name = "Imported",
                                Details = "Imported",
                                LocationCode = hasMain.LocationCode,
                                TaskDate = DateTime.Now,
                                ExternalReferenceEntries = [..newMail.Select(ticket => new ExternalReferenceEntry()
                                                                {
                                                                    PartitionKey = ticket.PartitionKey,
                                                                    RowKey = ticket.RowKey,
                                                                    TableName = ticket.TableName,
                                                                    EntityType = ticket.EntityType,
                                                                    Date = ticket.Date,
                                                                    Action = ActionType.External,
                                                                    Accepted = false,
                                                                    ExternalGroupId = ticket.ThreadId
                                                                })
                                ],
                            });
                        }
                        catch (Exception ex)
                        {
                            logger.LogError(new EventId(33), ex, $@"Exception : {ex.Message}. {ex.StackTrace}");
                        }
                    }
                }
            }
        }
    }
}
