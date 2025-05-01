using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RepositoryContract.CommitedOrders;
using RepositoryContract.Orders;
using WebApi.Models;
using WebApi.Services;

namespace WebApi.Controllers
{
    public class WorkerController : WebApiController2
    {
        private readonly StructuraReport _structuraReport;
        private readonly ICommitedOrdersRepository _commitedOrdersRepository;
        private IOrdersRepository _ordersRepository;
        public WorkerController(
            StructuraReport structuraReport,
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
            var commitedOrders = await _commitedOrdersRepository.GetCommitedOrders(DateTime.MinValue);
            var perDay = commitedOrders.OrderBy(x => x.TransportDate).GroupBy(t => t.TransportDate.HasValue ? t.TransportDate.Value.ToString("dd-MM-yy") : "0");
            var workItems = new List<WorkerPriorityList>();

            foreach (var days in perDay)
            {
                var model = new WorkerPriorityList([], days.Key);
                foreach (var commited in days)
                {
                    model.WorkItems.Add(new WorkItem()
                    {
                        CodProdus = commited.CodProdus,
                        Cantitate = commited.Cantitate,
                        CodLocatie = commited.CodLocatie,
                        DeliveryDate = commited.TransportDate,
                        NumarComanda = commited.NumarComanda,
                        NumeProdus = commited.NumeProdus,
                    });
                }

                model.WorkDisplayItems.AddRange(await _structuraReport.GenerateReport(workerName, model, model));
                workItems.Add(model);
            }

            return Ok(workItems);
        }
    }
}
