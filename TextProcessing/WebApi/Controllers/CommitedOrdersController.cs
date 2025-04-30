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
using WebApi.Services;
using EntityDto.CommitedOrders;
using WorkSheetServices;
using RepositoryContract.Transports;
using WordDocument.Services;
using ServiceImplementation;

namespace WebApi.Controllers
{
    [Authorize(Roles = "admin, basic")]
    public class CommitedOrdersController : WebApiController2
    {
        private readonly ICommitedOrdersRepository commitedOrdersRepository;
        private readonly IProductCodeRepository productCodeRepository;
        private readonly ITaskRepository taskRepository;
        private readonly IDataKeyLocationRepository keyLocationRepository;
        private readonly StructuraReport _structuraReport;
        private readonly SimpleReport _simpleReport;
        private readonly ICryptoService _cryptoService;
        ICacheManager<CommitedOrderEntry> cacheManager;
        IMetadataService metadataService;

        public CommitedOrdersController(
            ILogger<CommitedOrdersController> logger,
            ICommitedOrdersRepository commitedOrdersRepository,
            IProductCodeRepository productCodeRepository,
            ITaskRepository taskRepository,
            ICacheManager<CommitedOrderEntry> cacheManager,
            IMetadataService metadataService,
            StructuraReport structuraReport,
            SimpleReport simpleReport,
            ICryptoService cryptoService,
            IDataKeyLocationRepository keyLocationRepository,
            IMapper mapper) : base(logger, mapper)
        {
            this.commitedOrdersRepository = commitedOrdersRepository;
            this.taskRepository = taskRepository;
            this.productCodeRepository = productCodeRepository;
            this.cacheManager = cacheManager;
            this.metadataService = metadataService;
            this.keyLocationRepository = keyLocationRepository;
            _structuraReport = structuraReport;
            _simpleReport = simpleReport;
            _cryptoService = cryptoService;
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
            }
            catch (Exception ex)
            {
                logger.LogError(new EventId(69), ex, "GetCommitedOrders");
            }

            return Ok(CommitedOrdersResponse.From(orders, tickets ?? [], synonimLocations ?? [], tasks ?? [], productLinkWeights ?? [], weights ?? []));
        }

        [HttpPost("get")]
        public async Task<IActionResult> GetCommitedOrdersIds(int[] ids)
        {

            IList<ProductCodeStatsEntry>? productLinkWeights = null;
            IList<ProductStatsEntry>? weights = null;
            try
            {

                productLinkWeights = [.. (await productCodeRepository.GetProductCodeStatsEntry()).Where(x => x.RowKey == "Greutate")];
                weights = [.. (await productCodeRepository.GetProductStats()).Where(x => x.PropertyCategory == "Greutate")];
            }
            catch (Exception ex)
            {
                logger.LogError(new EventId(69), ex, "GetCommitedOrders");
            }
            var orders = await commitedOrdersRepository.GetCommitedOrders(ids);

            return Ok(CommitedOrdersResponse.From(orders, [], [], [], productLinkWeights ?? [], weights ?? []));
        }

        [HttpPost("reclamatii")]
        public async Task<IActionResult> ExportReclamatii(ComplaintDocument document)
        {
            try
            {
                using var reportStream = await _simpleReport.GetSimpleReport("Reclamatii", document.LocationCode, document,
                    new() {
                        { "identity", nameof(TransportEntry) },
                        { "identityD", document.TransportId?.ToString() }
                    });
                await WriteStreamToResponse(reportStream, @$"Reclamatie-{document.LocationName}.docx", wordType);
                return new EmptyResult();
            }
            catch (Exception ex)
            {
                logger.LogError(new EventId(69), ex, "ExportReclamatii");
                return StatusCode(500, "An error occurred while exporting the report.");
            }
        }

        [HttpPost("reclamatii-multiple/{repotName}")]
        public async Task<IActionResult> ExportStructuraReportMultiple(ComplaintDocument[] commitedOrders, string repotName)
        {
            var groupedCommited = GetOrdersGrouped(commitedOrders, t => t.LocationCode);

            Stream tempStream = TempFileHelper.CreateTempFile();
            using (var wordDoc = await DocXServiceHelper.CreateEmptyDoc(tempStream))
            {

                for (int i = 0; i < groupedCommited.Count; i++)
                {
                    try
                    {
                        var complaint = groupedCommited[i];
                        var reportStream = await _simpleReport.GetSimpleReport(repotName, complaint.LocationCode, complaint, null);
                        await DocXServiceHelper.CloneDocument(reportStream, wordDoc.MainDocumentPart!, 1, _cryptoService.GetMd5);
                        DocXServiceHelper.AddPageBreak(wordDoc.MainDocumentPart!);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(new EventId(69), ex, @$"ExportStructuraReport : {groupedCommited[i].LocationCode}");
                    }
                }
                wordDoc.MainDocumentPart!.Document.Save();
                wordDoc.Dispose();
                tempStream.Seek(0, SeekOrigin.Begin);
                await WriteStreamToResponse(tempStream, @$"Transport-PV.docx", wordType);
            }

            return new EmptyResult();
        }
        

        [HttpPost("pv-report/{reportName}")]
        public async Task<IActionResult> ExportStructuraReport(string reportName, CommitedOrdersBase commitedOrder)
        {
            try
            {
                using var reportStream = await _structuraReport.GenerateReport(reportName, commitedOrder.CodLocatie, commitedOrder,
                    new() {
                        { "identity", nameof(TransportEntry) },
                        { "identityD", commitedOrder.TransportId?.ToString() }
                    });
                await WriteStreamToResponse(reportStream, $"Transport-{reportName}-{commitedOrder.NumeLocatie}.docx", wordType);

                return new EmptyResult();
            }
            catch (Exception ex)
            {
                logger.LogError(new EventId(69), ex, "ExportStructuraReport");
                return StatusCode(500, "An error occurred while exporting the report.");
            }
        }

        [HttpPost("merge-commited-orders")]
        public async Task<IActionResult> ExportDispozitii(string[] internalNumber)
        {
            internalNumber = [.. internalNumber.Order()];
            var items = await commitedOrdersRepository.GetCommitedOrders(internalNumber.Select(int.Parse).ToArray());
            var synonimLocations = (await keyLocationRepository.GetLocations())
                .Where(t => t.MainLocation && !string.IsNullOrWhiteSpace(t.ShortName) && items.Any(o => o.CodLocatie == t.LocationCode))
                .DistinctBy(t => t.LocationCode)
                .ToDictionary(x => x.LocationCode, x => x.ShortName);

            var missing = internalNumber.Except(items.DistinctBy(t => t.NumarIntern).Select(t => t.NumarIntern));

            if (missing.Any()) return NotFound(string.Join(", ", missing));

            Response.Headers["Content-Disposition"] = $@"attachment; filename={string.Join("_", internalNumber.Take(5))}.xlsx";
            Response.ContentType = "application/octet-stream";

            await WorkbookReportsService.GenerateReport(
                               items.Cast<CommitedOrder>().ToList(),
                               t => synonimLocations.ContainsKey(t.CodLocatie) ? synonimLocations[t.CodLocatie] : t.CodLocatie.ToUpperInvariant(),
                               t => t.CodProdus.StartsWith("MPB") ? t.CodProdus.Substring(0, 5) : string.Concat(t.CodProdus.AsSpan(0, 2), t.CodProdus.AsSpan(4, 1)),
                               t => t.NumeProdus, Response.BodyWriter.AsStream());
            return new EmptyResult();
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

        private List<T> GetOrdersGrouped<T>(T[] commitedOrders, Func<T, string> groupBy)
        {
            var groups = commitedOrders.GroupBy(groupBy).ToList();
            List<T> result = new();

            for (int i = 0; i < groups.Count; i++)
            {
                var commited = groups[i];
                result.Add(commited.First());
                if (commited.First() is ComplaintDocument cDoc)
                {
                    cDoc.complaintEntries.AddRange(commited.Skip(1).Cast<ComplaintDocument>().SelectMany(t => t.complaintEntries));
                }
            }

            return result;
        }
    }
}