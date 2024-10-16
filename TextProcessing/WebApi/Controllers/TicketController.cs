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
    using Microsoft.ServiceFabric.Services.Remoting.Client;
    using Microsoft.ServiceFabric.Services.Remoting.V2.FabricTransport.Client;
    using YahooFeeder;
    using RepositoryContract;

    [Authorize(Roles = "partener, admin")]
    public class TicketController : WebApiController2
    {
        private ITicketEntryRepository ticketEntryRepository;
        private IDataKeyLocationRepository dataKeyLocationRepository;
        private IStorageService storageService;
        private ITaskRepository taskRepository;

        private static readonly ServiceProxyFactory serviceProxy = new ServiceProxyFactory((c) =>
        {
            return new FabricTransportServiceRemotingClientFactory();
        });

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
            await taskRepository.SaveTask(new TaskEntry()
            {
                Details = complaint.Tickets[0].Description,
                Name = complaint.NrComanda,
                LocationCode = complaint.DataKey
            });
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
        [HttpPost("eml/{partitionKey}/{rowKey}")]
        public async Task<IActionResult> GetEmlMessage(string partitionKey, string rowKey)
        {
            var proxy = serviceProxy.CreateServiceProxy<IYahooFeeder>(new Uri("fabric:/TextProcessing/YahooTFeederType"));
            var path = await proxy.DownloadAll([TableEntityPK.From(partitionKey, rowKey)]);

            if (string.IsNullOrEmpty(path[0].Path))
            {
                return BadRequest();
            }
            else
            {
                return File(storageService.Access(path.First().Path, out var contentType), contentType);
            }
        }
    }
}
