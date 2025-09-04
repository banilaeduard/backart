using EntityDto;
using EntityDto.Tasks;
using Microsoft.Extensions.DependencyInjection;
using RepositoryContract;
using RepositoryContract.DataKeyLocation;
using RepositoryContract.Tasks;
using RepositoryContract.Tickets;
using ServiceInterface.Storage;

namespace PollerRecurringJob.MailOperations
{
    internal static class AddNewMailToExistingTasks
    {
        private static EventGridListener<List<AddMailToTask>> _listener;
        private static PollerRecurringJob _jobContext;
        private static ITaskRepository repo;
        private static IWorkflowTrigger client;

        internal static Task StartListening(PollerRecurringJob jobContext)
        {
            _jobContext = jobContext;
            _listener = new EventGridListener<List<AddMailToTask>>(
                    Environment.GetEnvironmentVariable("service_bus_conn")!,
                    "posting",
                    Process
                );

            client = _jobContext.provider.GetRequiredService<IWorkflowTrigger>();
            repo = jobContext.provider.GetRequiredService<ITaskRepository>();
            return _listener.StartAsync();
        }

        internal static async Task StopListening()
        {
            await _listener.DisposeAsync();
            _listener = null;
            _jobContext = null;
            client = null;
            repo = null;
        }

        private static async Task Process(List<AddMailToTask> messages, CancellationToken token)
        {
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
                IDataKeyLocationRepository locationRepository = _jobContext.provider.GetRequiredService<IDataKeyLocationRepository>()!;
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

                            await client.Trigger("movemailto", new MoveToMessage<TableEntityPK>
                            {
                                DestinationFolder = "_PENDING_",
                                Items = task.ExternalReferenceEntries.Select(x => TableEntityPK.From(x.PartitionKey!, x.RowKey!))
                            });
                        }
                        catch (Exception ex)
                        {
                            ActorEventSource.Current.ActorMessage(_jobContext, $@"Exception : {ex.Message}. {ex.StackTrace}");
                        }
                    }
                }
            }
        }
    }
}