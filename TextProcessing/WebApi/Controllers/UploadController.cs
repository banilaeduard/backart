using DataAccess.Context;
using EntityDto;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RepositoryContract.Orders;
using WorkSheetServices;

namespace WebApi.Controllers
{
    [Authorize(Roles = "admin")]
    public class UploadController : WebApiController2
    {
        private ImportsDbContext imports;
        private CodeDbContext codeDbContext;
        private IOrdersRepository ordersRepository;

        public UploadController(
            ILogger<UploadController> logger,
            ImportsDbContext imports,
            CodeDbContext codeDbContext,
            IOrdersRepository ordersRepository
            ) : base(logger)
        {
            this.imports = imports;
            this.codeDbContext = codeDbContext;
            this.ordersRepository = ordersRepository;
        }

        [HttpPost("orders"), DisableRequestSizeLimit]
        public async Task<IActionResult> UploadOrders()
        {
            var file = Request.Form.Files[0];

            using (var stream = file.OpenReadStream())
            {
                var items = WorkbookReader.ReadWorkBook<ComandaVanzare>(stream, 4);
                await ordersRepository.ImportOrders(items);
            }

            return Ok();
        }

        [HttpGet("orders")]
        public async Task<IActionResult> GetOrders()
        {
            return Ok(await ordersRepository.GetOrders());
        }
    }
}
