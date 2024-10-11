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
    [Authorize(Roles = "admin")]
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
            IOrdersRepository ordersRepository) : base(logger)
        {
            this.productCodeRepository = productCodeRepository;
            this.commitedOrdersRepository = commitedOrdersRepository;
            this.ordersRepository = ordersRepository;
            this.taskRepository = taskRepository;
            this.ticketEntryRepository = ticketEntryRepository;
            this.keyLocationRepository = keyLocationRepository;
        }

        [HttpGet]
        public async Task<IActionResult> GetTasks()
        {
            var taskLists = await taskRepository.GetActiveTasks();

            var tickets = await ticketEntryRepository.GetAll();
            var synonimLocations = (await keyLocationRepository.GetLocations()).Where(t => taskLists.Any(o => o.LocationCode == t.LocationCode)).ToList();

            return Ok(TaskModel.From(taskLists, tickets, synonimLocations));
        }

        [HttpDelete("{taskId}")]
        public async Task<IActionResult> deleteTasks(int taskId)
        {
            await taskRepository.DeleteTask(taskId);
            return Ok();
        }

        [HttpPost("tickets")]
        public async Task<IActionResult> CreateFromTickets(TicketModel[] tickets)
        {
            await taskRepository.InsertFromTicketEntries([..tickets.Select(t => new TicketEntity()
            {
                LocationCode = t.Location ?? "",
                Description = t.Description,
                PartitionKey = t.PartitionKey,
                RowKey = t.RowKey,
                From = t.From
            })]);
            return Ok();
        }
    }
}
