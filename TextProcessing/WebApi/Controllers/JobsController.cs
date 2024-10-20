using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.ServiceFabric.Actors.Client;
using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Services.Remoting.Client;
using Microsoft.ServiceFabric.Services.Remoting.V2.FabricTransport.Client;
using RepositoryContract.CommitedOrders;
using RepositoryContract.Imports;
using RepositoryContract.Orders;
using System.Fabric;
using YahooFeeder;
using PollerRecurringJob.Interfaces;
using AutoMapper;

namespace WebApi.Controllers
{
    [Authorize(Roles = "admin")]
    public class JobsController : WebApiController2
    {
        private MailSettings mailSettings;
        private StatelessServiceContext context;
        private IOrdersRepository ordersRepository;
        private ICommitedOrdersRepository commitedOrdersRepository;
        private IImportsRepository importsRepository;

        private static readonly ServiceProxyFactory serviceProxy = new ServiceProxyFactory((c) =>
        {
            return new FabricTransportServiceRemotingClientFactory();
        });

        public JobsController(
            ILogger<JobsController> logger,
            StatelessServiceContext context,
            IOrdersRepository ordersRepository,
            ICommitedOrdersRepository commitedOrdersRepository,
            IImportsRepository importsRepository,
            IMapper mapper,
            MailSettings settings) : base(logger, mapper)
        {
            this.mailSettings = settings;
            this.context = context;
            this.commitedOrdersRepository = commitedOrdersRepository;
            this.ordersRepository = ordersRepository;
            this.importsRepository = importsRepository;
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

        [HttpGet("trigger")]
        public async Task<IActionResult> Trigger()
        {
            try
            {
                ServiceEventSource.Current.ServiceMessage(this.context, "Service name is {0}", this.context.ServiceName.ToString());
                var proxy = serviceProxy.CreateServiceProxy<IYahooFeeder>(new Uri("fabric:/TextProcessing/YahooTFeederType"));

                await proxy.Get();

                return Ok();
            }
            catch (Exception ex)
            {
                ServiceEventSource.Current.ServiceMessage(this.context, ex.Message);
                return Ok(ex);
            }
        }

        [HttpGet("orders")]
        public async Task<IActionResult> TriggerOrders()
        {
            try
            {
                var proxy = ActorProxy.Create<IPollerRecurringJob>(new ActorId(0), new Uri("fabric:/TextProcessing/PollerRecurringJobActorService"));
                await proxy.Sync();

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
