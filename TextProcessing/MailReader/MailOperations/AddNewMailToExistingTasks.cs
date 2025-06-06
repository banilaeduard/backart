﻿using EntityDto;
using RepositoryContract.Tasks;
using RepositoryContract.Tickets;
using ServiceInterface.Storage;
using EntityDto.Tasks;
using Microsoft.Extensions.DependencyInjection;
using RepositoryContract.DataKeyLocation;
using RepositoryContract;

namespace MailReader.MailOperations
{
    internal static class AddNewMailToExistingTasks
    {
        internal static async Task Execute(MailReader jobContext, List<AddMailToTask> items)
        {
            if (!items.Any()) return;

            ITaskRepository repo = jobContext.provider.GetRequiredService<ITaskRepository>();
            var tasks = await repo.GetTasks(TaskInternalState.Open);
            var externalRefs = tasks.SelectMany(x => x.ExternalReferenceEntries.Where(t => t.EntityType == nameof(TicketEntity))).ToList().OrderBy(t => t.TaskId);

            var items2 = items.Where(newMail => externalRefs.Any(er => er.ExternalGroupId.Equals(newMail.ThreadId))).ToList();

            // update only active tasks
            foreach (var task in tasks)
            {
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
                IDataKeyLocationRepository locationRepository = jobContext.provider.GetRequiredService<IDataKeyLocationRepository>()!;
                var mainLocs = (await locationRepository.GetLocations()).Where(loc => loc.MainLocation).ToList();

                foreach (var newMail in newMails)
                {
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

                            IWorkflowTrigger client = jobContext.provider.GetRequiredService<IWorkflowTrigger>();
                            await client.Trigger("movemailto", new MoveToMessage<TableEntityPK>
                            {
                                DestinationFolder = "_PENDING_",
                                Items = task.ExternalReferenceEntries.Select(x => TableEntityPK.From(x.PartitionKey!, x.RowKey!))
                            });
                        }
                        catch (Exception ex)
                        {
                            ActorEventSource.Current.ActorMessage(jobContext, $@"Exception : {ex.Message}. {ex.StackTrace}");
                        }
                    }
                }
            }
        }
    }
}

