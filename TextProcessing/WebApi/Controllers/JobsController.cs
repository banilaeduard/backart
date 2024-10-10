using Microsoft.AspNetCore.Mvc;
using Microsoft.ServiceFabric.Services.Remoting.Client;
using Microsoft.ServiceFabric.Services.Remoting.V2.FabricTransport.Client;
using RepositoryContract.CommitedOrders;
using RepositoryContract.Imports;
using RepositoryContract.Orders;
using System.Fabric;
using YahooFeeder;

namespace WebApi.Controllers
{
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
            MailSettings settings) : base(logger)
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
                var commitedD = await commitedOrdersRepository.GetLastSyncDate();
                var orderD = await ordersRepository.GetLastSyncDate();
                var sourceOrders = await importsRepository.GetImportCommitedOrders(commitedD, orderD);

                commitedD = commitedD ?? (sourceOrders.commited?.MaxBy(t => t.DataDocument).DataDocument);
                orderD = orderD ?? (sourceOrders.orders?.MaxBy(t => t.DataDoc).DataDoc);

                await commitedOrdersRepository.ImportCommitedOrders(sourceOrders.commited, commitedD.Value);
                await ordersRepository.ImportOrders(sourceOrders.orders, orderD.Value);

                return Ok(new { orders = sourceOrders.orders.Count, commited = sourceOrders.commited.Count });
            }
            catch (Exception ex)
            {
                ServiceEventSource.Current.ServiceMessage(this.context, ex.Message);
                return Ok(ex);
            }
        }
    }
}
