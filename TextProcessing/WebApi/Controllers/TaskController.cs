using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RepositoryContract.CommitedOrders;
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

        public TaskController(
            ILogger<ReportsController> logger,
            ICommitedOrdersRepository commitedOrdersRepository,
            IProductCodeRepository productCodeRepository,
            ITaskRepository taskRepository,
            IOrdersRepository ordersRepository) : base(logger)
        {
            this.productCodeRepository = productCodeRepository;
            this.commitedOrdersRepository = commitedOrdersRepository;
            this.ordersRepository = ordersRepository;
            this.taskRepository = taskRepository;
        }

        [HttpGet]
        public async Task<IActionResult> GetTasks()
        {
            var taskLists = await taskRepository.GetActiveTasks();
            return Ok(taskLists);
        }

        [HttpPost("tickets")]
        public async Task<IActionResult> CreateFromTickets(TicketModel[] tickets)
        {
            await taskRepository.InsertFromTicketEntries([..tickets.Select(t => new TicketEntity()
            {
                LocationCode = t.Location ?? "",
                Description = t.Description,
                PartitionKey = t.PartitionKey,
                RowKey = t.RowKey
            })]);
            return Ok();
        }
    }
}
