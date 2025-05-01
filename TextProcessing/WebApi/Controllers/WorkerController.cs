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
            DateTime? All = DateTime.Now.AddMonths(-2);
            var commitedOrders = (await _commitedOrdersRepository.GetCommitedOrders(All)).Where(t => !t.Livrata);
            var orders = await _ordersRepository.GetOrders();
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

                var items = (await _structuraReport.GenerateReport(workerName, model, model)).Where(t => t.Count > 0);
                if (items.Any())
                {
                    model.WorkDisplayItems.AddRange(items);
                    workItems.Add(model);
                }
            }

            var orderItems = new List<WorkerPriorityList>();
            foreach (var orderGroup in orders.GroupBy(t => t.CodArticol))
            {
                var model = new WorkerPriorityList([], "orders");
                var orderSample = orderGroup.First();
                model.WorkItems.Add(new WorkItem()
                {
                    CodProdus = orderSample.CodArticol,
                    Cantitate = orderGroup.Sum(x => x.Cantitate),
                    NumeProdus = orderSample.NumeArticol,
                });
                var items = (await _structuraReport.GenerateReport(workerName, model, model)).Where(t => t.Count > 0);
                if (items.Any())
                {
                    model.WorkDisplayItems.AddRange(items);
                    orderItems.Add(model);
                }
            }

            return Ok(new { workItems, orderItems });
        }
    }
}
