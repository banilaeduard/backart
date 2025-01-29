using AutoMapper;
using EllipticCurve;
using EntityDto;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RepositoryContract.CommitedOrders;
using RepositoryContract.DataKeyLocation;
using RepositoryContract.ProductCodes;
using RepositoryContract.Tasks;
using RepositoryContract.Tickets;
using WebApi.Models;
using WorkSheetServices;

namespace WebApi.Controllers
{
    [Authorize(Roles = "admin, basic")]
    public class CommitedOrdersController : WebApiController2
    {
        const string contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

        private ICommitedOrdersRepository commitedOrdersRepository;
        private ITicketEntryRepository ticketEntryRepository;
        private IDataKeyLocationRepository keyLocationRepository;
        private IProductCodeRepository productCodeRepository;
        private ITaskRepository taskRepository;

        public CommitedOrdersController(
            ILogger<CommitedOrdersController> logger,
            ICommitedOrdersRepository commitedOrdersRepository,
            ITicketEntryRepository ticketEntryRepository,
            IDataKeyLocationRepository keyLocationRepository,
            IProductCodeRepository productCodeRepository,
            ITaskRepository taskRepository,
            IMapper mapper) : base(logger, mapper)
        {
            this.commitedOrdersRepository = commitedOrdersRepository;
            this.ticketEntryRepository = ticketEntryRepository;
            this.keyLocationRepository = keyLocationRepository;
            this.taskRepository = taskRepository;
            this.productCodeRepository = productCodeRepository;
        }

        [HttpGet]
        public async Task<IActionResult> GetCommitedOrders()
        {
            var orders = await commitedOrdersRepository.GetCommitedOrders();

            var productLinkWeights = (await productCodeRepository.GetProductCodeStatsEntry()).Where(x => x.RowKey == "Greutate");
            var weights = (await productCodeRepository.GetProductStats()).Where(x => x.PropertyCategory == "Greutate");

            var tickets = await ticketEntryRepository.GetAll();
            var synonimLocations = (await keyLocationRepository.GetLocations()).Where(t => orders.Any(o => o.CodLocatie == t.LocationCode)).ToList();
            var tasks = await taskRepository.GetTasks(TaskInternalState.All);

            return Ok(CommitedOrdersResponse.From(orders, tickets, synonimLocations, tasks, [.. productLinkWeights], [.. weights]));
        }

        [HttpPost("delivered/{internalNumber}")]
        public async Task<IActionResult> DeliverOrder(int internalNumber)
        {
            await commitedOrdersRepository.SetDelivered(internalNumber);
            return Ok();
        }

        [HttpPost("merge")]
        public async Task<IActionResult> ExportDispozitii(string[] internalNumber)
        {
            var items = await commitedOrdersRepository.GetCommitedOrders(t => internalNumber.Any(x => x == t.NumarIntern));
            var synonimLocations = (await keyLocationRepository.GetLocations())
                .Where(t => t.MainLocation && !string.IsNullOrWhiteSpace(t.ShortName) && items.Any(o => o.CodLocatie == t.LocationCode))
                .DistinctBy(t => t.LocationCode)
                .ToDictionary(x => x.LocationCode, x => x.ShortName);

            var missing = internalNumber.Except(items.DistinctBy(t => t.NumarIntern).Select(t => t.NumarIntern));

            if (missing.Any()) return NotFound(string.Concat(", ", missing));

            var reportData = WorkbookReportsService.GenerateReport(
                items.Cast<DispozitieLivrare>().ToList(),
                t => synonimLocations.ContainsKey(t.CodLocatie) ? synonimLocations[t.CodLocatie] : t.CodLocatie.ToUpperInvariant(),
                t => string.Concat(t.CodProdus.AsSpan(0, 2), t.CodProdus.AsSpan(4, 1)),
                t => t.CodProdus);

            return File(reportData, contentType);
        }
    }
}
