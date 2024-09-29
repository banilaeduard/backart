using DataAccess.Context;
using Microsoft.AspNetCore.Mvc;
using Microsoft.ServiceFabric.Services.Remoting.Client;
using Microsoft.ServiceFabric.Services.Remoting.V2.FabricTransport.Client;
using System.Fabric;
using YahooFeeder;

namespace WebApi.Controllers
{
    public class JobsController : WebApiController2
    {
        private MailSettings mailSettings;
        private StatelessServiceContext context;
        private static readonly ServiceProxyFactory serviceProxy = new ServiceProxyFactory((c) =>
        {
            return new FabricTransportServiceRemotingClientFactory();
        });

        public JobsController(
            ILogger<JobsController> logger,
            StatelessServiceContext context,
            MailSettings settings) : base(logger)
        {
            this.mailSettings = settings;
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
    }
}
