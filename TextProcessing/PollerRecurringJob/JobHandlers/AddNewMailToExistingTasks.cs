﻿using AzureServices;
using EntityDto;
using RepositoryContract.Tasks;
using RepositoryContract.Tickets;
using SqlTableRepository.Tasks;

namespace PollerRecurringJob.JobHandlers
{
    internal static class AddNewMailToExistingTasks
    {
        internal static async Task Execute(PollerRecurringJob jobContext)
        {
            var client = await QueueService.GetClient("addmailtotask");
            var messages = await client.ReceiveMessagesAsync(maxMessages: 32);
            AddMailToTask[] items = [.. messages.Value.Select(str => QueueService.Deserialize<AddMailToTask[]>(str.Body.ToString())).SelectMany(t => t!)];

            if (!items.Any()) return;

            TaskRepository repo = new TaskRepository();
            var tasks = await repo.GetTasks(TaskInternalState.Open);
            var externalRefs = tasks.SelectMany(x => x.ExternalReferenceEntries.Where(t => t.TableName == nameof(TicketEntity))).ToList().OrderBy(t => t.TaskId);

            var items2 = items.Where(newMail => externalRefs.Any(er => er.ExternalGroupId.Equals(newMail.ThreadId))).ToList();

            // update only active tasks
            foreach (var task in tasks)
            {
                // make sure we don't have the external mail attached
                var intersect = items2.Where(newMail => externalRefs.Any(er => er.TaskId == task.Id
                    && er.ExternalGroupId.Equals(newMail.ThreadId)
                    && $"{er.PartitionKey}_{er.RowKey}_{er.TableName}" != $"{newMail.PartitionKey}_{newMail.RowKey}_{newMail.TableName}"
                )).ToList();
                if (intersect.Any())
                {
                    task.ExternalReferenceEntries.AddRange(intersect.Select(ticket =>
                    new ExternalReferenceEntry()
                    {
                        PartitionKey = ticket.PartitionKey,
                        RowKey = ticket.RowKey,
                        TableName = ticket.TableName,
                        Date = ticket.Date,
                        Action = ActionType.External,
                        Accepted = false,
                        ExternalGroupId = ticket.ThreadId
                    }));

                    await repo.UpdateTask(task);
                }
            }

            foreach (var item in messages.Value)
            {
                await client.DeleteMessageAsync(item.MessageId, item.PopReceipt);
            }

            //var processQueue = await QueueService.GetClient("createtaskfrommail");
            //processQueue.SendMessage(QueueService.Serialize(items.Except(items2).ToArray()));
        }
    }
}