using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RepositoryContract.CommitedOrders;
using RepositoryContract.Orders;
using Services.Storage;
using WebApi.Models;

namespace WebApi.Controllers
{
    [Authorize(Roles = "admin")]
    public class CommitedOrdersController : WebApiController2
    {
        private IStorageService storageService;
        private ICommitedOrdersRepository commitedOrdersRepository;
        private IOrdersRepository ordersRepository;

        public CommitedOrdersController(
            ILogger<CommitedOrdersController> logger,
            IStorageService storageService,
            ICommitedOrdersRepository commitedOrdersRepository,
            IOrdersRepository ordersRepository) : base(logger)
        {
            this.storageService = storageService;
            this.commitedOrdersRepository = commitedOrdersRepository;
            this.ordersRepository = ordersRepository;
        }

        [HttpGet]
        public async Task<IActionResult> GetCommitedOrders()
        {
            List<CommitedOrdersResponse> result = new();
            var commitedOrders = await commitedOrdersRepository.GetCommitedOrders();
            foreach (var orderClient in commitedOrders.GroupBy(t => t.CodLocatie))
            {
                var minDate = orderClient.Min(t => t.DataDocument);
                var progressOrders = await ordersRepository.GetOrders(t => t.CodLocatie == orderClient.Key && t.DataDoc <= minDate, ComandaVanzareEntry.GetProgressTableName());
                var pendingOrders = await ordersRepository.GetOrders(t => t.CodLocatie == orderClient.Key && t.DataDoc <= minDate);

                foreach (var order in orderClient)
                {
                    result.Add(new()
                    {
                        Entry = order,
                        Progress = progressOrders.Where(t => t.CodArticol == order.CodProdus).ToList(),
                        Pending = pendingOrders.Where(t => t.CodArticol == order.CodProdus).ToList()
                    }
                        );
                }
            }

            return Ok(result.ToList());
        }
    }
}
