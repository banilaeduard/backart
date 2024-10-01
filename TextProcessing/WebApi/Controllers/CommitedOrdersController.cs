using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RepositoryContract.CommitedOrders;
using RepositoryContract.Imports;
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
    }
}
