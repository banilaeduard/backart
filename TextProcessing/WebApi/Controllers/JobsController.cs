using AutoMapper;
using Azure.Data.Tables;
using AzureServices;
using Dapper;
using EntityDto;
using EntityDto.Config;
using MailReader.Interfaces;
using MetadataService.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using PollerRecurringJob.Interfaces;
using RepositoryContract.ProductCodes;
using RepositoryContract.Tickets;
using ServiceInterface.Storage;
using WebApi.Models;
using WorkSheetServices;

namespace WebApi.Controllers
{
    [Authorize(Roles = "admin")]
    public class JobsController : WebApiController2
    {
        private readonly IWorkflowTrigger _workflowTrigger;
        private readonly IStorageService _storageService;
        private readonly IProductCodeRepository _productCodeRepository;
        private readonly TableStorageService _tableStorageService;

        public JobsController(
            ILogger<JobsController> logger,
            IWorkflowTrigger workflowTrigger,
            IStorageService storageService,
            TableStorageService tableStorageService,
            IProductCodeRepository productCodeRepository,
            IMapper mapper) : base(logger, mapper)
        {
            _workflowTrigger = workflowTrigger;
            _storageService = storageService;
            _productCodeRepository = productCodeRepository;
            _tableStorageService = tableStorageService;
        }

        [HttpGet()]
        public IActionResult Statuses()
        {
            return Ok(
                null
                );
        }

        [HttpPost("ArchiveMails")]
        public async Task<IActionResult> ArchiveMails(TableEntryModel[] archiveEntries)
        {
            var proxy2 = GetActor<IPollerRecurringJob>("ArchiveMails");
            foreach (var batch in archiveEntries.GroupBy(t => t.PartitionKey))
            {
                await _workflowTrigger.Trigger<List<ArchiveMail>>("archivemail", [..batch.Select(t => new ArchiveMail()
                    {
                        FromTable = nameof(TicketEntity),
                        PartitionKey = t.PartitionKey,
                        RowKey = t.RowKey,
                        ToTable = $@"{nameof(TicketEntity)}Archive"
                    })]);
            }
            await proxy2.ArchiveMail();
            return Ok();
        }

        [HttpGet("orders")]
        public async Task<IActionResult> TriggerOrders()
        {
            var proxy2 = GetActor<IPollerRecurringJob>("source1");
            return Ok(await proxy2.SyncOrdersAndCommited());
        }

        [HttpGet("bust/{collection}")]
        public async Task<IActionResult> BustCache(string collection)
        {
            await GetService<IMetadataServiceFabric>().DeleteDataAsync(collection);

            return Ok();
        }

        [HttpGet("cache-keys")]
        public async Task<IActionResult> CacheKeys()
        {
            var result = await GetService<IMetadataServiceFabric>().GetAllCollectionKeys();
            return Ok(result);
        }

        [HttpPost("upload/{categoryName}/{clientName}")]
        public async Task<IActionResult> UploadFile([FromForm] IFormFile file, string categoryName, string clientName)
        {
            if (string.IsNullOrWhiteSpace(categoryName))
                return BadRequest("Category name is required.");

            if (file == null || file.Length == 0)
                return BadRequest("No file uploaded.");
            var fPath = $@"cfg/{SanitizeFileName(file.FileName)}";

            await _storageService.WriteTo(fPath, file.OpenReadStream(), true);
            await using var stream = _storageService.Access(fPath, out var _);
            var result = WorkbookReader.ReadWorkBook(stream, new Settings<ClientPriceList>()
            {
                FirstDataRow = 2,
            }, sheetName: clientName);

            var partitionKey = AzureTableUtils.Sanitize(clientName) + "_" + categoryName;
            IDictionary<string, ProductClientCode> dbItems;

#if DEBUG
            dbItems = (await _productCodeRepository.GetProductClientCodes(clientName)).Distinct().ToDictionary(t => t.partneritemkey, t => t);
#else
            await using var conn = new SqlConnection(ProjectKeys.KeyCollection.ExternalServer);
            var sql = $@"select itemkey, pi.partneritemkey FROM dbo.item it
                                                                join dbo.partneritem pi on pi.itemid = it.objectid and pi.valid = 1
                                                                join dbo.partner p on p.objectid = pi.partnerid and p.partnername = @partnerName
                                                                where (itemkey like 'MPPL%' or itemkey like 'MPAL%' or itemkey like 'MPOP%' or itemkey like 'MPKA%') and barcode is not null;";

            dbItems = (await conn.QueryAsync<ProductClientCode>(sql, new { @partnerName = clientName })).ToDictionary(t => t.partneritemkey, t => t);
#endif
            var resultItems = result.items.IntersectBy(dbItems.Select(t => t.Key), x => x.CodProdusClient).ToList();

            var query = TableClient.CreateQueryFilter($"Level eq {1} and NumeCodificare ge {""}");
            var productCodes = _tableStorageService.Query<ProductCodeEntry>(query, nameof(ProductCodeEntry)).DistinctBy(t => t.Code).ToDictionary(t => t.Code, t => t);

            List<ProductStatsEntry> productStats2 = [.. resultItems.Select(it => new ProductStatsEntry() {
                    PartitionKey = partitionKey,
                    RowKey = AzureTableUtils.Sanitize(it.CodProdusClient),
                    PropertyCategory = categoryName,
                    PropertyName = it.CodProdusClient,
                    PropertyType = "decimal",
                    PropertyValue = it.PretClient.ToString("0.00"),
                })];

            await _productCodeRepository.CreateProductStats(productStats2);
            var productStats = productStats2.ToDictionary(t => t.RowKey, t => t);

            await _productCodeRepository.CreateProductCodeStatsEntry([.. resultItems.Select(it => new ProductCodeStatsEntry() {
                    PartitionKey = partitionKey,
                    RowKey = AzureTableUtils.Sanitize(it.CodProdusClient),
                    ProductPartitionKey = productCodes.GetValueOrDefault((string)dbItems[it.CodProdusClient].itemkey)?.PartitionKey,
                    ProductRowKey = productCodes.GetValueOrDefault((string)dbItems[it.CodProdusClient].itemkey)?.RowKey,
                    StatsPartitionKey = productStats.GetValueOrDefault(AzureTableUtils.Sanitize(it.CodProdusClient))?.PartitionKey,
                    StatsRowKey = productStats.GetValueOrDefault(AzureTableUtils.Sanitize(it.CodProdusClient))?.RowKey,
                })]);

            return Ok();
        }
    }
}
