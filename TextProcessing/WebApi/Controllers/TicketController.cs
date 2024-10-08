namespace WebApi.Controllers
{
    using System.Threading.Tasks;
    using System.Linq;

    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.Extensions.Logging;

    using RepositoryContract.Tickets;
    using global::WebApi.Models;
    using global::Services.Storage;

    [Authorize(Roles = "partener, admin")]
    public class TicketController : WebApiController2
    {
        private ITicketEntryRepository ticketEntryRepository;
        private IStorageService storageService;
        public TicketController(
            ITicketEntryRepository ticketEntryRepository,
            IStorageService storageService,
            ILogger<TicketController> logger) : base(logger)
        {
            this.ticketEntryRepository = ticketEntryRepository;
            this.storageService = storageService;
        }

        [HttpGet("{page}/{pageSize}")]
        public async Task<IActionResult> GetAll(int page, int pageSize)
        {
            var complaints = await ticketEntryRepository.GetAll();
            complaints = [..complaints.Where(t => !t.IsDeleted)];

            var result = complaints.GroupBy(T => T.ThreadId);
            var paged = result.OrderByDescending(t => t.Max(t => t.CreatedDate))
                               .Skip((page - 1) * pageSize)
                               .Take(pageSize)
                               .Select(t => TicketSeriesModel.from([.. t]))
                               .ToList();

            return Ok(new
            {
                count = result.Count(),
                complaints = paged
            });
        }

        [HttpPost("delete")]
        public async Task<IActionResult> Delete(TicketModel[] tickets)
        {
            var items = (await ticketEntryRepository.GetAll()).Where(t => tickets.Any(x => x.RowKey == t.RowKey && x.PartitionKey == t.PartitionKey));
            foreach (var item in items)
            {
                item.IsDeleted = true;
            }
            await ticketEntryRepository.Save([.. items]);
            return Ok(await ticketEntryRepository.GetAll());
        }

        [HttpPost]
        public async Task<IActionResult> SaveComplaint(TicketSeriesModel complaint)
        {
            //await ticketEntryRepository.Save(complaint);
            return Ok();
        }
    }
}
