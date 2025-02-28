using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.ServiceFabric.Actors.Client;
using Microsoft.ServiceFabric.Actors;
using System.Fabric;
using AutoMapper;
using MailReader.Interfaces;
using PollerRecurringJob.Interfaces;

namespace WebApi.Controllers
{
    [Authorize(Roles = "admin")]
    public class JobsController : WebApiController2
    {
        private StatelessServiceContext context;

        public JobsController(
            ILogger<JobsController> logger,
            StatelessServiceContext context,
            IMapper mapper) : base(logger, mapper)
        {
            this.context = context;
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
                Console.WriteLine(ex.Message);
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
                ServiceEventSource.Current.ServiceMessage(this.context, ex.Message);
                return Ok(ex);
            }
        }
    }
}
