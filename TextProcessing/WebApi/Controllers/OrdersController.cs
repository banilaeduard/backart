using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RepositoryContract.Imports;
using RepositoryContract.Orders;
using RepositoryContract.ProductCodes;
using WebApi.Models;

namespace WebApi.Controllers
{
    [Authorize(Roles = "admin, basic")]
    public class OrdersController : WebApiController2
    {
        private IOrdersRepository ordersRepository;
        private IProductCodeRepository productCodeRepository;

        public OrdersController(
            ILogger<OrdersController> logger,
            IOrdersRepository ordersRepository,
            IProductCodeRepository productCodeRepository,
            IMapper mapper
            ) : base(logger, mapper)
        {
            this.ordersRepository = ordersRepository;
            this.productCodeRepository = productCodeRepository;
        }

        [HttpGet()]
        public async Task<IActionResult> GetOrders()
        {
            List<ProductCodeStatsEntry> productLinkWeights = [];
            List<ProductStatsEntry> weights = [];
            try
            {
                productLinkWeights = [.. (await productCodeRepository.GetProductCodeStatsEntry()).Where(x => x.RowKey == "Greutate")];
                weights = [.. (await productCodeRepository.GetProductStats()).Where(x => x.PropertyCategory == "Greutate")];
            }
            catch (Exception ex)
            {
                logger.LogError(new EventId(69), ex, "ExportStructuraReport");
            }

            var odersModel = (await ordersRepository.GetOrders()).Select(product => mapper.Map<OrderModel>(product).Weight(weights.FirstOrDefault(w =>
            {
                var pw = productLinkWeights.FirstOrDefault(x => x.PartitionKey == product.CodArticol);
                return w.RowKey == pw?.StatsRowKey && w.PartitionKey == pw?.StatsPartitionKey;
            })));
            return Ok(odersModel);
        }
    }
}
