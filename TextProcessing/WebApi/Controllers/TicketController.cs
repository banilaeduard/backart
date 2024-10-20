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
    using AutoMapper;

    [Authorize(Roles = "admin, basic")]
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
            IMapper mapper,
            ILogger<TicketController> logger) : base(logger, mapper)
        {
            this.ticketEntryRepository = ticketEntryRepository;
            this.storageService = storageService;
            this.dataKeyLocationRepository = dataKeyLocationRepository;
            this.taskRepository = taskRepository;
        }

        [HttpGet]
        [Authorize(Roles = "admin")]
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
        [Authorize(Roles = "admin")]
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
        [Authorize(Roles = "admin")]
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
        [Authorize(Roles = "admin")]
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

        [HttpPost("details")]
        public async Task<IActionResult> GetTicketDetails(TableEntryModel[] entries)
        {
            var proxy = serviceProxy.CreateServiceProxy<IYahooFeeder>(new Uri("fabric:/TextProcessing/YahooTFeederType"));
            var path = await proxy.DownloadAll(entries.Select(x => new TableEntityPK() { PartitionKey = x.PartitionKey, RowKey = x.RowKey }).ToArray());

            if (string.IsNullOrEmpty(path[0].Path))
            {
                return BadRequest();
            }
            else
            {
                var attachments = await ticketEntryRepository.GetAllAttachments();
                return Ok(attachments.Where(a => entries.Any(e => e.PartitionKey == a.RefPartition && e.RowKey == a.RefKey)).Select(mapper.Map<AttachmentModel>));
            }
        }

        [HttpPost("eml")]
        public async Task<IActionResult> GetEmlMessage(TableEntryModel entry)
        {
            var attachments = await ticketEntryRepository.GetAllAttachments(entry.RowKey);
            if (!attachments.Any())
            {
                var proxy = serviceProxy.CreateServiceProxy<IYahooFeeder>(new Uri("fabric:/TextProcessing/YahooTFeederType"));
                var path = await proxy.DownloadAll([new TableEntityPK() { PartitionKey = entry.PartitionKey, RowKey = entry.RowKey }]);
                if (string.IsNullOrEmpty(path[0].Path))
                {
                    return BadRequest();
                }
                else
                {
                    return File(storageService.Access(path.First().Path, out var contentType2), contentType2);
                }
            }
            var eml = attachments.FirstOrDefault(t => t.RowKey.Contains("eml"));
            return File(storageService.Access(eml.Data, out var contentType), contentType);

        }
    }
}