﻿using EntityDto;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RepositoryContract.CommitedOrders;
using RepositoryContract.Imports;
using RepositoryContract.Orders;
using Services.Storage;
using WebApi.Models;
using WorkSheetServices;

namespace WebApi.Controllers
{
    [Authorize(Roles = "admin")]
    public class CommitedOrdersController : WebApiController2
    {
        const string contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

        private IStorageService storageService;
        private ICommitedOrdersRepository commitedOrdersRepository;
        private IOrdersRepository ordersRepository;
        private IImportsRepository importsRepository;

        public CommitedOrdersController(
            ILogger<CommitedOrdersController> logger,
            IStorageService storageService,
            ICommitedOrdersRepository commitedOrdersRepository,
            IImportsRepository importsRepository,
            IOrdersRepository ordersRepository) : base(logger)
        {
            this.storageService = storageService;
            this.commitedOrdersRepository = commitedOrdersRepository;
            this.ordersRepository = ordersRepository;
            this.importsRepository = importsRepository;
        }

        [HttpGet]
        public async Task<IActionResult> GetCommitedOrders()
        {
            var orders = await commitedOrdersRepository.GetCommitedOrders();
            return Ok(CommitedOrdersResponse.From(orders));
        }

        [HttpPost("sync"), DisableRequestSizeLimit]
        public async Task<IActionResult> SyncCommitedOrders()
        {
            var sourceOrders = await importsRepository.GetImportCommitedOrders(await commitedOrdersRepository.GetLastSyncDate());
            await commitedOrdersRepository.ImportCommitedOrders(sourceOrders);
            var orders = await commitedOrdersRepository.GetCommitedOrders();

            return Ok(CommitedOrdersResponse.From(orders));
        }

        [HttpPost("delivered/{internalNumber}")]
        public async Task<IActionResult> DeliverOrder(int internalNumber)
        {
            await commitedOrdersRepository.SetDelivered(internalNumber);
            return Ok();
        }

        [HttpPost("merge")]
        public async Task<IActionResult> ExportDispozitii(string[] internalNumber)
        {
            var items = await commitedOrdersRepository.GetCommitedOrders(t => internalNumber.Any(x => x == t.NumarIntern));

            var missing = internalNumber.Except(items.DistinctBy(t => t.NumarIntern).Select(t => t.NumarIntern));

            if (missing.Any()) return NotFound(string.Concat(", ", missing));

            var reportData = WorkbookReportsService.GenerateReport(
                items.Cast<DispozitieLivrare>().ToList(),
                t => string.Format("{0} - {1}", t.NumarIntern.ToString(), t.NumeLocatie),
                t => string.Concat(t.CodProdus.AsSpan(0, 2), t.CodProdus.AsSpan(4, 1)),
                t => t.CodProdus);

            return File(reportData, contentType);
        }
    }
}
