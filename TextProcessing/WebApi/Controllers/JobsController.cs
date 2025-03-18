using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.ServiceFabric.Actors.Client;
using Microsoft.ServiceFabric.Actors;
using AutoMapper;
using MailReader.Interfaces;
using PollerRecurringJob.Interfaces;
using Microsoft.ServiceFabric.Services.Remoting.Client;
using Microsoft.ServiceFabric.Services.Remoting.V2.FabricTransport.Client;
using Microsoft.ServiceFabric.Services.Client;
using MetadataService.Interfaces;

namespace WebApi.Controllers
{
    [Authorize(Roles = "admin")]
    public class JobsController : WebApiController2
    {
        internal ServiceProxyFactory serviceProxy = new ServiceProxyFactory((c) =>
        {
            return new FabricTransportServiceRemotingClientFactory();
        });

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
            var serviceUri = new Uri("fabric:/TextProcessing/MetadataService");

            var service = serviceProxy.CreateServiceProxy<IMetadataServiceFabric>(serviceUri, ServicePartitionKey.Singleton);
            await service.DeleteDataAsync(collection);

            return Ok();
        }

        [HttpGet("cache-keys")]
        public async Task<IActionResult> CacheKeys()
        {
            var serviceUri = new Uri("fabric:/TextProcessing/MetadataService");

            var service = serviceProxy.CreateServiceProxy<IMetadataServiceFabric>(serviceUri, ServicePartitionKey.Singleton);
            var result = await service.GetAllCollectionKeys();

            return Ok(result);
        }
    }
}
