using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RepositoryContract.DataKeyLocation;
using RepositoryContract.Tasks;
using RepositoryContract.Tickets;
using WebApi.Models;
using WebApi.Services;

namespace WebApi.Controllers
{
    [Authorize(Roles = "admin, basic")]
    public class TaskController : WebApiController2
    {
        const string contentType = "application/vnd.openxmlformats-officedocument.wordprocessingml.document";
        private ITaskRepository taskRepository;
        private ITicketEntryRepository ticketEntryRepository;
        private IDataKeyLocationRepository keyLocationRepository;
        private ReclamatiiReport reclamatiiReport;

        public TaskController(
            ILogger<ReportsController> logger,
            ITaskRepository taskRepository,
            ITicketEntryRepository ticketEntryRepository,
            IDataKeyLocationRepository keyLocationRepository,
            ReclamatiiReport reclamatiiReport,
            IMapper mapper) : base(logger, mapper)
        {
            this.taskRepository = taskRepository;
            this.ticketEntryRepository = ticketEntryRepository;
            this.keyLocationRepository = keyLocationRepository;
            this.reclamatiiReport = reclamatiiReport;
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
            var newTask = await taskRepository.SaveTask(task.ToTaskEntry());
            return Ok(TaskModel.From([newTask], await ticketEntryRepository.GetAll(), [.. await keyLocationRepository.GetLocations()]).First());
        }

        [HttpPost("close")]
        public async Task<IActionResult> MarkAsClosed(TaskModel task)
        {
            var tEntry = task.ToTaskEntry();
            tEntry.IsClosed = true;
            return Ok(await taskRepository.UpdateTask(task.ToTaskEntry()));
        }

        [HttpDelete("{taskId}")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> DeleteTasks(int taskId)
        {
            await taskRepository.DeleteTask(taskId);
            return Ok();
        }

        [HttpPatch]
        public async Task<IActionResult> UpdateTask(TaskModel task)
        {
            var newTask = await taskRepository.UpdateTask(task.ToTaskEntry());
            return Ok(TaskModel.From([newTask], await ticketEntryRepository.GetAll(), [.. await keyLocationRepository.GetLocations()]).First());
        }

        [HttpPost("reclamatii")]
        public async Task<IActionResult> ExportReclamatii(ComplaintDocument document)
        {
            return File(await reclamatiiReport.GenerateReport(document), contentType);
        }
    }
}
