using AutoMapper;
using EntityDto.CommitedOrders;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RepositoryContract.Imports;
using RepositoryContract.Orders;
using RepositoryContract.ProductCodes;
using WebApi.Models;
using WorkSheetServices;

namespace WebApi.Controllers
{
    [Authorize(Roles = "admin, basic")]
    public class OrdersController : WebApiController2
    {
        private IOrdersRepository ordersRepository;
        private IImportsRepository importsRepository;
        private IProductCodeRepository productCodeRepository;

        public OrdersController(
            ILogger<OrdersController> logger,
            IOrdersRepository ordersRepository,
            IImportsRepository importsRepository,
            IProductCodeRepository productCodeRepository,
            IMapper mapper
            ) : base(logger, mapper)
        {
            this.ordersRepository = ordersRepository;
            this.importsRepository = importsRepository;
            this.productCodeRepository = productCodeRepository;
        }

        [HttpPost("upload"), DisableRequestSizeLimit]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> UploadOrders()
        {
            var file = Request.Form.Files[0];

            using (var stream = file.OpenReadStream())
            {
                var items = WorkbookReader.ReadWorkBook<Order>(stream, 4);
                await ordersRepository.ImportOrders(items, DateTime.Now);
            }

            return Ok();
        }

        [HttpGet()]
        public async Task<IActionResult> GetOrders()
        {
            var productLinkWeights = (await productCodeRepository.GetProductCodeStatsEntry()).Where(x => x.RowKey == "Greutate");
            var weights = (await productCodeRepository.GetProductStats()).Where(x => x.PropertyCategory == "Greutate");

            var odersModel = (await ordersRepository.GetOrders()).Select(product => mapper.Map<OrderModel>(product).Weight(weights.FirstOrDefault(w =>
            {
                var pw = productLinkWeights.FirstOrDefault(x => x.PartitionKey == product.CodArticol);
                return w.RowKey == pw?.StatsRowKey && w.PartitionKey == pw?.StatsPartitionKey;
            })));
            return Ok(odersModel);
        }
    }
}
