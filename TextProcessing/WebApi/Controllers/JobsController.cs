using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.ServiceFabric.Actors.Client;
using Microsoft.ServiceFabric.Actors;
using AutoMapper;
using MailReader.Interfaces;
using PollerRecurringJob.Interfaces;
using MetadataService.Interfaces;

namespace WebApi.Controllers
{
    [Authorize(Roles = "admin")]
    public class JobsController : WebApiController2
    {
        public JobsController(
            ILogger<JobsController> logger,
            IMapper mapper) : base(logger, mapper)
        {
        }

        [HttpGet()]
        public IActionResult Statuses()
        {
            try
            {
                return Ok(
                    null
                    );
            }
            catch (Exception ex)
            {
                logger.LogError(new EventId(69), ex, nameof(JobsController));
                return NotFound();
            }
        }

        [HttpGet("orders")]
        public async Task<IActionResult> TriggerOrders()
        {
            try
            {
                var proxy = ActorProxy.Create<IMailReader>(new ActorId("source1"), new Uri("fabric:/TextProcessing/MailReaderActorService"));
                var proxy2 = ActorProxy.Create<IPollerRecurringJob>(new ActorId("source2"), new Uri("fabric:/TextProcessing/PollerRecurringJobActorService"));

                await Task.WhenAll(proxy.FetchMails(), proxy2.SyncOrdersAndCommited());

                return Ok();
            }
            catch (Exception ex)
            {
                logger.LogError(new EventId(69), ex, nameof(JobsController));
                return BadRequest(ex.Message);
            }
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
