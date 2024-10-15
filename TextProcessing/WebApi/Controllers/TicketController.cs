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
    using RepositoryContract.DataKeyLocation;
    using RepositoryContract.Tasks;

    [Authorize(Roles = "partener, admin")]
    public class TicketController : WebApiController2
    {
        private ITicketEntryRepository ticketEntryRepository;
        private IDataKeyLocationRepository dataKeyLocationRepository;
        private IStorageService storageService;
        private ITaskRepository taskRepository;

        public TicketController(
            ITicketEntryRepository ticketEntryRepository,
            IDataKeyLocationRepository dataKeyLocationRepository,
            IStorageService storageService,
            ITaskRepository taskRepository,
            ILogger<TicketController> logger) : base(logger)
        {
            this.ticketEntryRepository = ticketEntryRepository;
            this.storageService = storageService;
            this.dataKeyLocationRepository = dataKeyLocationRepository;
            this.taskRepository = taskRepository;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var complaints = await ticketEntryRepository.GetAll();

            var result = complaints.GroupBy(T => T.ThreadId);

            var externalRefs = await taskRepository.GetExternalReferences();
            var paged = result.Select(t => TicketSeriesModel.from([.. t], externalRefs))
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
            return Ok();
        }

        [HttpPost]
        public async Task<IActionResult> Save(TicketSeriesModel complaint)
        {
            await ticketEntryRepository.Save([..complaint.Tickets.Select(t => new TicketEntity()
            {
                CreatedDate = DateTime.Now.ToUniversalTime(),
                Description = t.Description,
                From = t.From,
                NrComanda = complaint.NrComanda,
                TicketSource = "Manual",
                PartitionKey = t.PartitionKey,
                RowKey = t.RowKey,
                IsDeleted = false,
                ThreadId = t.RowKey
            })]);
            return Ok();
        }

        [HttpPost("saveLocation/{partitionKey}/{rowKey}")]
        public async Task<IActionResult> SaveComplaintLocation(TicketSeriesModel complaint, string partitionKey, string rowKey)
        {
            var locations = await dataKeyLocationRepository.GetLocations();
            var location = locations.First(t => t.PartitionKey == partitionKey && t.RowKey == rowKey);

            var items = (await ticketEntryRepository.GetAll()).Where(t => complaint.Tickets.Any(x => x.RowKey == t.RowKey && x.PartitionKey == t.PartitionKey));
            foreach (var item in items)
            {
                item.LocationCode = location.LocationCode;
                item.LocationPartitionKey = location.PartitionKey;
                item.LocationRowKey = location.RowKey;
            }

            await ticketEntryRepository.Save([.. items]);
            return Ok();
        }
    }
}