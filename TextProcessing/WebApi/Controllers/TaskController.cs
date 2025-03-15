using AutoMapper;
using EntityDto;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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
            ILogger<ReportsController> logger,
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

            var tickets = await ticketEntryRepository.GetAll();
            var synonimLocations = (await keyLocationRepository.GetLocations()).Where(t => taskLists.Any(o => o.LocationCode == t.LocationCode)).ToList();
            return Ok(TaskModel.From(taskLists, tickets, synonimLocations));
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

            return Ok(TaskModel.From([newTask], await ticketEntryRepository.GetAll(), [.. await keyLocationRepository.GetLocations()]).First());
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
                    await workflowTrigger.Trigger("movemailto", new MoveToMessage<TableEntityPK>
                    {
                        DestinationFolder = "Archive",
                        Items = newTask.ExternalReferenceEntries.Where(t => t.TableName == nameof(TicketEntity)).Select(x => TableEntityPK.From(x.PartitionKey, x.RowKey))
                    });
                }
            }

            return Ok(TaskModel.From([newTask], await ticketEntryRepository.GetAll(), [.. await keyLocationRepository.GetLocations()]).First());
        }
    }
}
