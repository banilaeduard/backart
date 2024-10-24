using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RepositoryContract.CommitedOrders;
using RepositoryContract.DataKeyLocation;
using RepositoryContract.Orders;
using RepositoryContract.ProductCodes;
using RepositoryContract.Tasks;
using RepositoryContract.Tickets;
using WebApi.Models;

namespace WebApi.Controllers
{
    [Authorize(Roles = "admin, basic")]
    public class TaskController : WebApiController2
    {
        const string contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
        private ICommitedOrdersRepository commitedOrdersRepository;
        private IOrdersRepository ordersRepository;
        private IProductCodeRepository productCodeRepository;
        private ITaskRepository taskRepository;
        private ITicketEntryRepository ticketEntryRepository;
        private IDataKeyLocationRepository keyLocationRepository;

        public TaskController(
            ILogger<ReportsController> logger,
            ICommitedOrdersRepository commitedOrdersRepository,
            IProductCodeRepository productCodeRepository,
            ITaskRepository taskRepository,
            ITicketEntryRepository ticketEntryRepository,
            IDataKeyLocationRepository keyLocationRepository,
            IOrdersRepository ordersRepository,
            IMapper mapper) : base(logger, mapper)
        {
            this.productCodeRepository = productCodeRepository;
            this.commitedOrdersRepository = commitedOrdersRepository;
            this.ordersRepository = ordersRepository;
            this.taskRepository = taskRepository;
            this.ticketEntryRepository = ticketEntryRepository;
            this.keyLocationRepository = keyLocationRepository;
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
    }
}
