namespace WebApi.Controllers
{
    using System.Threading.Tasks;
    using System.Linq;

    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.Extensions.Logging;

    using RepositoryContract.Tickets;
    using global::WebApi.Models;
    using RepositoryContract.DataKeyLocation;
    using RepositoryContract.Tasks;
    using RepositoryContract;
    using AutoMapper;
    using MailReader.Interfaces;
    using ServiceInterface.Storage;
    using RepositoryContract.ExternalReferenceGroup;
    using EntityDto.Tasks;

    [Authorize(Roles = "admin, basic")]
    public class TicketController : WebApiController2
    {
        private readonly ITicketEntryRepository ticketEntryRepository;
        private readonly IDataKeyLocationRepository dataKeyLocationRepository;
        private readonly IStorageService storageService;
        private readonly ITaskRepository taskRepository;
        private readonly IExternalReferenceGroupRepository externalReferenceGroupRepository;

        public TicketController(
            ITicketEntryRepository ticketEntryRepository,
            IDataKeyLocationRepository dataKeyLocationRepository,
            IStorageService storageService,
            ITaskRepository taskRepository,
            IMapper mapper,
            IExternalReferenceGroupRepository externalReferenceGroupRepository,
            ILogger<TicketController> logger) : base(logger, mapper)
        {
            this.ticketEntryRepository = ticketEntryRepository;
            this.storageService = storageService;
            this.dataKeyLocationRepository = dataKeyLocationRepository;
            this.taskRepository = taskRepository;
            this.externalReferenceGroupRepository = externalReferenceGroupRepository;
        }

        [HttpGet]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> GetAll()
        {
            var complaints = await ticketEntryRepository.GetAll();

            var result = complaints.GroupBy(T => T.ThreadId);

            var externalRefs = await externalReferenceGroupRepository.GetExternalReferences();

            var paged = result.Select(t => TicketSeriesModel.from([.. t], [.. externalRefs.Select(mapper.Map<ExternalReference>)]))
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
            await ticketEntryRepository.DeleteEntity([.. items]);
            foreach (var ticket in items)
            {
                var attachments = await ticketEntryRepository.GetAllAttachments(ticket.RowKey);
                await ticketEntryRepository.DeleteEntity([.. attachments]);
                await ticketEntryRepository.Save([.. attachments], $@"{nameof(AttachmentEntry)}ARCHIVE");
            }
            await ticketEntryRepository.Save([.. items], $@"{nameof(TicketEntity)}ARCHIVE");
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
            var attachments = await ticketEntryRepository.GetAllAttachments();
            var relatedAttachments = attachments.Where(a => entries.Any(e => e.PartitionKey == a.RefPartition && e.RowKey == a.RefKey))
                .Select(mapper.Map<AttachmentModel>).ToList();
            var missing = entries.Where(e => !relatedAttachments.Any(x => x.RefKey == e.RowKey && x.RefPartition == e.PartitionKey)).ToList();

            if (missing.Any())
            {
                var proxy = GetActor<IMailReader>("source1");
                await proxy.DownloadAll([.. missing.Select(x => new TableEntityPK() {
                        PartitionKey = x.PartitionKey,
                        RowKey = x.RowKey
                    })]);
                attachments = await ticketEntryRepository.GetAllAttachments();
                relatedAttachments = [.. attachments.Where(a => entries.Any(e => e.PartitionKey == a.RefPartition && e.RowKey == a.RefKey)).Select(mapper.Map<AttachmentModel>)];
            }

            return Ok(relatedAttachments.Select(mapper.Map<AttachmentModel>));
        }

        [HttpPost("eml")]
        public async Task<IActionResult> GetEmlMessage(TableEntryModel entry)
        {
            var attachments = await ticketEntryRepository.GetAllAttachments(entry.RowKey);
            if (!attachments.Any())
            {
                var proxy = GetActor<IMailReader>("source1");
                await proxy.DownloadAll([ new TableEntityPK() {
                    PartitionKey = entry.PartitionKey,
                    RowKey = entry.RowKey,
                }]);

                attachments = await ticketEntryRepository.GetAllAttachments(entry.RowKey);
                var attachment = attachments.First(x => x.ContentType == "eml");
                return File(storageService.Access(attachment.Data, out var contentType2), contentType2);
            }
            var eml = attachments.FirstOrDefault(t => t.RowKey.Contains("eml"));
            return File(storageService.Access(eml.Data, out var contentType), contentType ?? "application/text");
        }
    }
}