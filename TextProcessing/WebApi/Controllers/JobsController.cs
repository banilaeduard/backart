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
            var proxy = GetActor<IMailReader>("source1");
            var proxy2 = GetActor<IPollerRecurringJob>("source2");

            await Task.WhenAll(proxy.FetchMails(), proxy2.SyncOrdersAndCommited());

            return Ok();
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

            var partitionKey = AzureTableKeySanitizer.Sanitize(clientName) + "_" + categoryName;
            await using var conn = new SqlConnection(ProjectKeys.KeyCollection.ExternalServer);
            var sql = $@"select itemkey, pi.partneritemkey FROM dbo.item it
                                                                join dbo.partneritem pi on pi.itemid = it.objectid and pi.valid = 1
                                                                join dbo.partner p on p.objectid = pi.partnerid and p.partnername = @partnerName
                                                                where (itemkey like 'MPPL%' or itemkey like 'MPAL%' or itemkey like 'MPOP%' or itemkey like 'MPKA%') and barcode is not null;";

            var dbItems = (await conn.QueryAsync<dynamic>(sql, new { @partnerName = clientName })).ToDictionary(t => t.partneritemkey, t => new
            {
                t.itemkey,
                t.partneritemkey
            });

            var resultItems = result.items.IntersectBy(dbItems.Select(t => t.Key), x => x.CodProdusClient).ToList();

            var tableStorageService = new TableStorageService();
            var productCodes = tableStorageService.Query<ProductCodeEntry>(@$"{nameof(ProductCodeEntry.Level)} eq 1", nameof(ProductCodeEntry)).ToDictionary(t => t.Code, t => t);

            await _productCodeRepository.CreateProductStats([.. resultItems.Select(it => new ProductStatsEntry() {
                    PartitionKey = partitionKey,
                    RowKey = AzureTableKeySanitizer.Sanitize(it.CodProdusClient),
                    PropertyCategory = categoryName,
                    PropertyName = it.CodProdusClient,
                    PropertyType = "decimal",
                    PropertyValue = it.PretClient.ToString("0.00"),
                })]);
            var productStats = tableStorageService.Query<ProductStatsEntry>(TableClient.CreateQueryFilter(@$"PartitionKey eq {partitionKey}"), nameof(ProductStatsEntry)).ToDictionary(t => t.RowKey, t => t);

            await _productCodeRepository.CreateProductCodeStatsEntry([.. resultItems.Select(it => new ProductCodeStatsEntry() {
                    PartitionKey = partitionKey,
                    RowKey = AzureTableKeySanitizer.Sanitize(it.CodProdusClient),
                    ProductPartitionKey = productCodes.GetValueOrDefault((string)dbItems[it.CodProdusClient].itemkey)?.PartitionKey,
                    ProductRowKey = productCodes.GetValueOrDefault((string)dbItems[it.CodProdusClient].itemkey)?.RowKey,
                    StatsPartitionKey = productStats.GetValueOrDefault(AzureTableKeySanitizer.Sanitize(it.CodProdusClient))?.PartitionKey,
                    StatsRowKey = productStats.GetValueOrDefault(AzureTableKeySanitizer.Sanitize(it.CodProdusClient))?.RowKey,
                })]);

            return Ok();
        }
    }
}
