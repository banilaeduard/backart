using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.ServiceFabric.Services.Client;
using Microsoft.ServiceFabric.Services.Remoting.Client;
using Microsoft.ServiceFabric.Services.Remoting.V2.FabricTransport.Client;
using RepositoryContract.CommitedOrders;
using RepositoryContract.Orders;
using RepositoryContract.Transports;
using RepositoryServices;
using RepositoryServices.Models;
using V2.Interfaces;
using WebApi.Models;

namespace WebApi.Controllers
{
    public class WorkerController : WebApiController2
    {
        private readonly StructuraReportWriter _structuraReport;
        private readonly ICommitedOrdersRepository _commitedOrdersRepository;
        private IOrdersRepository _ordersRepository;
        internal ServiceProxyFactory serviceProxy = new ServiceProxyFactory((c) =>
        {
            return new FabricTransportServiceRemotingClientFactory();
        });

        public WorkerController(
            StructuraReportWriter structuraReport,
            ICommitedOrdersRepository commitedOrdersRepository,
            IOrdersRepository ordersRepository,
            ILogger<WorkerController> logger, IMapper mapper) : base(logger, mapper)
        {
            _structuraReport = structuraReport;
            _commitedOrdersRepository = commitedOrdersRepository;
            _ordersRepository = ordersRepository;
        }

        [HttpGet("{workerName}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetWorkLoad(string workerName)
        {
            var items = await GetService().GetItems(workerName);
            return Ok(items);
        }

        [HttpPost]
        public async Task<IActionResult> Publish()
        {
            await GetService().Publish();
            return Ok();
        }
        private IWorkLoadService GetService()
        {
            var serviceUri = new Uri("fabric:/TextProcessing/WorkLoadService");
            return serviceProxy.CreateServiceProxy<IWorkLoadService>(serviceUri, ServicePartitionKey.Singleton);
        }
    }
}
