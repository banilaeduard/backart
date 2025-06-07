using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AutoMapper;
using MailReader.Interfaces;
using PollerRecurringJob.Interfaces;
using MetadataService.Interfaces;
using WebApi.Models;
using ServiceInterface.Storage;
using EntityDto;
using RepositoryContract.Tickets;

namespace WebApi.Controllers
{
    [Authorize(Roles = "admin")]
    public class JobsController : WebApiController2
    {
        private readonly IWorkflowTrigger _workflowTrigger;
        public JobsController(
            ILogger<JobsController> logger,
            IWorkflowTrigger workflowTrigger,
            IMapper mapper) : base(logger, mapper)
        {
            _workflowTrigger = workflowTrigger;
        }

        [HttpGet()]
        public IActionResult Statuses()
        {
            return Ok(
                null
                );
        }

        [HttpPost("ArchiveMails")]
        public async Task<IActionResult> ArchiveMails(TableEntryModel[] archiveEntries)
        {
            var proxy2 = GetActor<IPollerRecurringJob>("ArchiveMails");
            foreach (var batch in archiveEntries.GroupBy(t => t.PartitionKey))
            {
                await _workflowTrigger.Trigger<List<ArchiveMail>>("archivemail", [..batch.Select(t => new ArchiveMail()
                    {
                        FromTable = nameof(TicketEntity),
                        PartitionKey = t.PartitionKey,
                        RowKey = t.RowKey,
                        ToTable = $@"{nameof(TicketEntity)}Archive"
                    })]);
            }
            await proxy2.ArchiveMail();
            return Ok();
        }

        [HttpGet("orders")]
        public async Task<IActionResult> TriggerOrders()
        {
            var proxy = GetActor<IMailReader>("source1");
            var proxy2 = GetActor<IPollerRecurringJob>("source2");

            await Task.WhenAll(proxy.FetchMails(), proxy2.SyncOrdersAndCommited());

            return Ok();
        }

        [HttpGet("bust/{collection}")]
        public async Task<IActionResult> BustCache(string collection)
        {
            await GetService<IMetadataServiceFabric>().DeleteDataAsync(collection);

            return Ok();
        }

        [HttpGet("cache-keys")]
        public async Task<IActionResult> CacheKeys()
        {
            var result = await GetService<IMetadataServiceFabric>().GetAllCollectionKeys();
            return Ok(result);
        }
    }
}
