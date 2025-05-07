using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.ServiceFabric.Services.Client;
using RepositoryContract.CommitedOrders;
using RepositoryContract.Orders;
using RepositoryServices;
using V2.Interfaces;

namespace WebApi.Controllers
{
    public class WorkerController : WebApiController2
    {

        public WorkerController(
            ILogger<WorkerController> logger, IMapper mapper) : base(logger, mapper)
        {
        }

        [HttpGet("{workerName}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetWorkLoad(string workerName)
        {
            var items = await GetService<IWorkLoadService>().GetItems(workerName);
            return Ok(items);
        }

        [HttpPost]
        public async Task<IActionResult> Publish()
        {
            await GetService<IWorkLoadService>().Publish();
            return Ok();
        }
    }
}
