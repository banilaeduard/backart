using AutoMapper;
using EntityDto;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PollerRecurringJob.Interfaces;
using RepositoryContract;
using RepositoryContract.DataKeyLocation;
using RepositoryContract.Tasks;
using RepositoryContract.Tickets;
using ServiceInterface.Storage;
using WebApi.Models;

namespace WebApi.Controllers
{
    [Authorize(Roles = "admin, basic")]
    public class TaskController : WebApiController2
    {
        private ITaskRepository taskRepository;
        private ITicketEntryRepository ticketEntryRepository;
        private IDataKeyLocationRepository keyLocationRepository;
        private IWorkflowTrigger workflowTrigger;

        public TaskController(
            ILogger<TaskController> logger,
            ITaskRepository taskRepository,
            ITicketEntryRepository ticketEntryRepository,
            IDataKeyLocationRepository keyLocationRepository,
            IWorkflowTrigger workflowTrigger,
            IMapper mapper) : base(logger, mapper)
        {
            this.taskRepository = taskRepository;
            this.ticketEntryRepository = ticketEntryRepository;
            this.keyLocationRepository = keyLocationRepository;
            this.workflowTrigger = workflowTrigger;
        }

        [HttpGet("{status}")]
        public async Task<IActionResult> GetTasks(string status)
        {
            var taskLists = await taskRepository.GetTasks(Enum.Parse<TaskInternalState>(status));

            var synonimLocations = (await keyLocationRepository.GetLocations()).Where(t => taskLists.Any(o => o.LocationCode == t.LocationCode)).ToList();
            return Ok(TaskModel.From(taskLists, await GetRelatedEntities(taskLists), synonimLocations));
        }

        [HttpPost("get")]
        public async Task<IActionResult> GetTasks(int[] taskIds)
        {
            var taskLists = await taskRepository.GetTasks(taskIds);

            var synonimLocations = (await keyLocationRepository.GetLocations()).Where(t => taskLists.Any(o => o.LocationCode == t.LocationCode)).ToList();
            return Ok(TaskModel.From(taskLists, await GetRelatedEntities(taskLists), synonimLocations));
        }

        [HttpPost]
        public async Task<IActionResult> SaveTask(TaskModel task)
        {
            var dbTask = task.ToTaskEntry();
            var newTask = await taskRepository.SaveTask(dbTask);

            if (task.ExternalMailReferences?.FirstOrDefault() != null)
            {
                await workflowTrigger.Trigger("movemailto", new MoveToMessage<TableEntityPK>
                {
                    DestinationFolder = "_PENDING_",
                    Items = task.ExternalMailReferences.SelectMany(x => x.Tickets).Select(x => TableEntityPK.From(x.PartitionKey!, x.RowKey!))
                });
            }

            return Ok(TaskModel.From([newTask], await GetRelatedEntities([newTask]), [.. await keyLocationRepository.GetLocations()]).First());
        }

        [HttpPost("mark-as-closed")]
        public async Task<IActionResult> MarkAsClosed(int[] taskIds)
        {
            await taskRepository.MarkAsClosed(taskIds);
            var tasks = await taskRepository.GetTasks(taskIds);

            var externalMail = tasks.SelectMany(x => x.ExternalReferenceEntries).Where(t => t.EntityType == nameof(TicketEntity)).ToList();

            if (externalMail.Any())
            {
                await workflowTrigger.Trigger("movemailto", new MoveToMessage<TableEntityPK>
                {
                    DestinationFolder = "Archive",
                    Items = externalMail.Select(x => TableEntityPK.From(x.PartitionKey!, x.RowKey!))
                });

                await workflowTrigger.Trigger("archivemail", externalMail.Select(x => new ArchiveMail()
                {
                    FromTable = nameof(TicketEntity),
                    ToTable = $@"{nameof(TicketEntity)}Archive",
                    PartitionKey = x.PartitionKey,
                    RowKey = x.RowKey,
                }).ToList()
                            );
                var actor = GetActor<IPollerRecurringJob>("taskcontroller");
                _ = Task.Run(async () => await actor.ArchiveMail());
            }
            return Ok(TaskModel.From(tasks, await GetRelatedEntities(tasks), [.. await keyLocationRepository.GetLocations()]));
        }

        [HttpDelete("{taskId}")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> DeleteTasks(int taskId)
        {
            await taskRepository.DeleteTask(taskId);
            return Ok();
        }

        [HttpDelete("{taskId}/delete/{partitionKey}/{rowKey}")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> DeleteTaskExternalRef(int taskId, string partitionKey, string rowKey)
        {
            await taskRepository.DeleteTaskExternalRef(taskId, partitionKey, rowKey);
            return Ok();
        }

        [HttpDelete("{taskId}/accept/{partitionKey}/{rowKey}")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> AcceptTaskExternalRef(int taskId, string partitionKey, string rowKey)
        {
            await taskRepository.AcceptExternalRef(taskId, partitionKey, rowKey);
            await workflowTrigger.Trigger("movemailto", new MoveToMessage<TableEntityPK>
            {
                DestinationFolder = "_PENDING_",
                Items = [TableEntityPK.From(partitionKey, rowKey)]
            });
            return Ok();
        }

        [HttpPatch]
        public async Task<IActionResult> UpdateTask(TaskModel task)
        {
            var newTask = await taskRepository.UpdateTask(task.ToTaskEntry());

            if (newTask.Actions.Any())
            {
                var lastAction = newTask.Actions.OrderByDescending(t => t.Created).First();
                if (lastAction.Description.Contains("Mark as closed"))
                {
                    var externalMail = newTask.ExternalReferenceEntries.Where(t => t.EntityType == nameof(TicketEntity)).ToList();

                    if (externalMail.Any())
                    {
                        await workflowTrigger.Trigger("movemailto", new MoveToMessage<TableEntityPK>
                        {
                            DestinationFolder = "Archive",
                            Items = newTask.ExternalReferenceEntries.Where(t => t.EntityType == nameof(TicketEntity)).Select(x => TableEntityPK.From(x.PartitionKey, x.RowKey))
                        });
                        await workflowTrigger.Trigger("archivemail",
                               newTask.ExternalReferenceEntries.Select(x => new ArchiveMail()
                               {
                                   FromTable = nameof(TicketEntity),
                                   ToTable = $@"{nameof(TicketEntity)}Archive",
                                   PartitionKey = x.PartitionKey,
                                   RowKey = x.RowKey,
                               }).ToList()
                            );

                        var actor = GetActor<IPollerRecurringJob>("taskcontroller");
                        _ = System.Threading.Tasks.Task.Run(async () => await actor.ArchiveMail());
                    }
                }
            }
            return Ok(TaskModel.From([newTask], await GetRelatedEntities([newTask]), [.. await keyLocationRepository.GetLocations()]).First());
        }

        private async Task<List<TicketEntity>> GetRelatedEntities(IList<TaskEntry> taskLists)
        {
            var mailExternalRefs = taskLists.SelectMany(t => t.ExternalReferenceEntries.Where(er => er.EntityType == nameof(TicketEntity)));

            List<TicketEntity> entries = new();
            foreach (var batch in mailExternalRefs.GroupBy(t => new { t.TableName, t.PartitionKey }))
            {
                entries.AddRange(await ticketEntryRepository.GetSome(batch.Key.TableName, batch.Key.PartitionKey, batch.Min(x => x.RowKey), batch.Max(x => x.RowKey)));
            }

            return entries;
        }
    }
}
