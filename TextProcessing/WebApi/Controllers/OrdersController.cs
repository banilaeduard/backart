﻿using AutoMapper;
using EntityDto;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RepositoryContract.Imports;
using RepositoryContract.Orders;
using WorkSheetServices;

namespace WebApi.Controllers
{
    [Authorize(Roles = "admin, basic")]
    public class OrdersController : WebApiController2
    {
        private IOrdersRepository ordersRepository;
        private IImportsRepository importsRepository;

        public OrdersController(
            ILogger<OrdersController> logger,
            IOrdersRepository ordersRepository,
            IImportsRepository importsRepository,
            IMapper mapper
            ) : base(logger, mapper)
        {
            this.ordersRepository = ordersRepository;
            this.importsRepository = importsRepository;
        }

        [HttpPost("upload"), DisableRequestSizeLimit]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> UploadOrders()
        {
            var file = Request.Form.Files[0];

            using (var stream = file.OpenReadStream())
            {
                var items = WorkbookReader.ReadWorkBook<ComandaVanzare>(stream, 4);
                await ordersRepository.ImportOrders(items, DateTime.Now);
            }

            return Ok();
        }

        [HttpGet()]
        public async Task<IActionResult> GetOrders()
        {
            return Ok(await ordersRepository.GetOrders());
        }
    }
}
