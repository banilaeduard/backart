namespace WebApi.Controllers
{
    using AutoMapper;
    using EntityDto;
    using EntityDto.Tasks;
    using global::WebApi.Models;
    using MailReader.Interfaces;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Logging;
    using PollerRecurringJob.Interfaces;
    using RepositoryContract;
    using RepositoryContract.DataKeyLocation;
    using RepositoryContract.ExternalReferenceGroup;
    using RepositoryContract.Tasks;
    using RepositoryContract.Tickets;
    using ServiceInterface.Storage;
    using System.Linq;
    using System.Threading.Tasks;

    [Authorize(Roles = "admin, basic")]
    public class TicketController : WebApiController2
    {
        private readonly ITicketEntryRepository ticketEntryRepository;
        private readonly IDataKeyLocationRepository dataKeyLocationRepository;
        private readonly IStorageService storageService;
        private readonly ITaskRepository taskRepository;
        private readonly IExternalReferenceGroupRepository externalReferenceGroupRepository;
        private readonly IWorkflowTrigger client;

        public TicketController(
            ITicketEntryRepository ticketEntryRepository,
            IDataKeyLocationRepository dataKeyLocationRepository,
            IStorageService storageService,
            ITaskRepository taskRepository,
            IMapper mapper,
            IWorkflowTrigger trigger,
            IExternalReferenceGroupRepository externalReferenceGroupRepository,
            ILogger<TicketController> logger) : base(logger, mapper)
        {
            this.ticketEntryRepository = ticketEntryRepository;
            this.storageService = storageService;
            this.dataKeyLocationRepository = dataKeyLocationRepository;
            this.taskRepository = taskRepository;
            this.externalReferenceGroupRepository = externalReferenceGroupRepository;
            this.client = trigger;
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
            await client.Trigger("archivemail",
                           tickets.Select(x => new ArchiveMail()
                           {
                               FromTable = nameof(TicketEntity),
                               ToTable = $@"{nameof(TicketEntity)}Archive",
                               PartitionKey = x.PartitionKey!,
                               RowKey = x.RowKey!,
                           }).ToList()
                        );

            await GetActor<IPollerRecurringJob>("ArchiveMails").ArchiveMail();
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