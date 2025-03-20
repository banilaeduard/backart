using AutoMapper;
using AzureTableRepository.CommitedOrders;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.ServiceFabric.Actors.Client;
using Microsoft.ServiceFabric.Actors;
using RepositoryContract.CommitedOrders;
using RepositoryContract.DataKeyLocation;
using RepositoryContract.ProductCodes;
using RepositoryContract.Tasks;
using RepositoryContract.Tickets;
using WebApi.Models;
using System.Globalization;
using ServiceInterface.Storage;
using ServiceInterface;
using PollerRecurringJob.Interfaces;

namespace WebApi.Controllers
{
    [Authorize(Roles = "admin, basic")]
    public class CommitedOrdersController : WebApiController2
    {
        private ICommitedOrdersRepository commitedOrdersRepository;
        private ITicketEntryRepository ticketEntryRepository;
        private IDataKeyLocationRepository keyLocationRepository;
        private IProductCodeRepository productCodeRepository;
        private ITaskRepository taskRepository;
        ICacheManager<CommitedOrderEntry> cacheManager;
        IMetadataService metadataService;

        public CommitedOrdersController(
            ILogger<CommitedOrdersController> logger,
            ICommitedOrdersRepository commitedOrdersRepository,
            ITicketEntryRepository ticketEntryRepository,
            IDataKeyLocationRepository keyLocationRepository,
            IProductCodeRepository productCodeRepository,
            ITaskRepository taskRepository,
            ICacheManager<CommitedOrderEntry> cacheManager, 
            IMetadataService metadataService,
            IMapper mapper) : base(logger, mapper)
        {
            this.commitedOrdersRepository = commitedOrdersRepository;
            this.ticketEntryRepository = ticketEntryRepository;
            this.keyLocationRepository = keyLocationRepository;
            this.taskRepository = taskRepository;
            this.productCodeRepository = productCodeRepository;
            this.cacheManager = cacheManager;
            this.metadataService = metadataService;
        }

        [HttpGet("{date}")]
        public async Task<IActionResult> GetCommitedOrders(string date)
        {
            var from = DateTime.Parse(date, CultureInfo.InvariantCulture);
            var orders = await commitedOrdersRepository.GetCommitedOrders(from);

            IList<ProductCodeStatsEntry>? productLinkWeights = null;
            IList<ProductStatsEntry>? weights = null;
            IList<TicketEntity>? tickets = null;
            IList<DataKeyLocationEntry>? synonimLocations = null;
            IList<TaskEntry>? tasks = null;
            try
            {
                tasks = await taskRepository.GetTasks(TaskInternalState.Open);

                productLinkWeights = [.. (await productCodeRepository.GetProductCodeStatsEntry()).Where(x => x.RowKey == "Greutate")];
                weights = [.. (await productCodeRepository.GetProductStats()).Where(x => x.PropertyCategory == "Greutate")];
                //var tickets = await ticketEntryRepository.GetAll();
                //var synonimLocations = (await keyLocationRepository.GetLocations()).Where(t => orders.Any(o => o.CodLocatie == t.LocationCode)).ToList();
            }
            catch (Exception ex)
            {
                logger.LogError(new EventId(69), ex, "GetCommitedOrders");
            }

            return Ok(CommitedOrdersResponse.From(orders, tickets ?? [], synonimLocations ?? [], tasks ?? [], productLinkWeights ?? [], weights ?? []));
        }

        [HttpPost("delivered/{internalNumber}/{numarAviz}")]
        public async Task<IActionResult> DeliverOrder(int internalNumber, int? numarAviz)
        {
            var commitedRepo = new CommitedOrdersRepository(cacheManager, metadataService);
            var commitedOrder = await commitedRepo.GetCommitedOrder(internalNumber);
            if (commitedOrder?.Count < 1)
            {
                var proxy = ActorProxy.Create<IPollerRecurringJob>(new ActorId(nameof(DeliverOrder)), new Uri("fabric:/TextProcessing/PollerRecurringJobActorService"));
                await proxy.SyncOrdersAndCommited();
            }
            await commitedRepo.SetDelivered(internalNumber, numarAviz);
            return Ok();
        }
    }
}
