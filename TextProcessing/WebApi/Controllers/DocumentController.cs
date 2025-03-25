using AutoMapper;
using AzureServices;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using EntityDto.CommitedOrders;
using Microsoft.AspNetCore.Mvc;
using RepositoryContract.CommitedOrders;
using RepositoryContract.DataKeyLocation;
using RepositoryContract.Report;
using RepositoryContract.Transports;
using ServiceImplementation;
using ServiceInterface.Storage;
using System.Text;
using WebApi.Models;
using WebApi.Services;
using WorkSheetServices;

namespace WebApi.Controllers
{
    public class DocumentController : WebApiController2
    {
        const string wordType = "application/vnd.openxmlformats-officedocument.wordprocessingml.document";

        private readonly IStorageService _storageService;
        private readonly IMetadataService _metadataService;
        private readonly IDataKeyLocationRepository _keyLocationRepository;
        private readonly ReclamatiiReport _reclamatiiReport;
        private readonly StructuraReport _structuraReport;
        private readonly ICommitedOrdersRepository _commitedOrdersRepository;
        private readonly ITransportRepository _transportRepository;
        private readonly ICryptoService _cryptoService;

        public DocumentController(ILogger<DocumentController> logger,
         AzureFileStorage storageService,
         IMetadataService metadataService,
         IDataKeyLocationRepository keyLocationRepository,
         ReclamatiiReport reclamatiiReport,
         StructuraReport structuraReport,
         IReportEntryRepository reportEntry,
         ICommitedOrdersRepository commitedOrdersRepository,
         ICryptoService cryptoService,
         ITransportRepository transportRepository,
         IMapper mapper) : base(logger, mapper)
        {
            _storageService = storageService;
            _metadataService = storageService;
            _keyLocationRepository = keyLocationRepository;
            _reclamatiiReport = reclamatiiReport;
            _structuraReport = structuraReport;
            _commitedOrdersRepository = commitedOrdersRepository;
            _transportRepository = transportRepository;
            _cryptoService = cryptoService;
        }

        [HttpPost("transport-papers")]
        public async Task<IActionResult> GenerateTransportPapers(TransportPapersModel transportPapers)
        {
            using Stream tempStream = TempFileHelper.CreateTempFile();
            using (var wordDoc = WordprocessingDocument.Create(tempStream, WordprocessingDocumentType.Document))
            {
                var part = wordDoc.AddMainDocumentPart();
                part.Document = new Document();
                part.Document.Append(new Body());
                var destStylesPart = part.AddNewPart<StyleDefinitionsPart>();
                destStylesPart.Styles = new Styles();
                destStylesPart.Styles.Save();

                transportPapers.CommitedOrders = transportPapers.CommitedOrders ?? [];
                for (int i = 0; i < transportPapers.CommitedOrders.Length; i++)
                {
                    var commited = transportPapers.CommitedOrders[i];
                    var fName = await GenerateAndWriteReport(commited, transportPapers.TransportId, transportPapers.DriverName);

                    await CloneDocument(fName, part, transportPapers.CommitedDuplicates, i > 0);
                }

                transportPapers.Complaints = transportPapers.Complaints ?? [];
                for (int i = 0; i < transportPapers.Complaints.Length; i++)
                {
                    var document = transportPapers.Complaints[i];
                    var fName = await GenerateAndWriteReport(document, transportPapers.TransportId, transportPapers.DriverName);

                    await CloneDocument(fName, part, transportPapers.ComplaintsDuplicates, i > 0 || transportPapers.CommitedOrders?.Any() == true);
                }

                part.Document.Save();
            }
            await WriteStreamToResponse(tempStream, @$"{transportPapers.TransportId}.docx", "application/octet-stream");

            return new EmptyResult();
        }

        [HttpPost("reclamatii")]
        public async Task<IActionResult> ExportReclamatii(ComplaintDocument document)
        {
            try
            {
                using var reportStream = await _reclamatiiReport.GenerateReport(document, null);
                await WriteStreamToResponse(reportStream, @$"Reclamatie-{document.LocationName}.docx", wordType);
                return new EmptyResult();
            }
            catch (Exception ex)
            {
                logger.LogError(new EventId(69), ex, "ExportReclamatii");
                return StatusCode(500, "An error occurred while exporting the report.");
            }
        }

        [HttpPost("pv-report/{reportName}")]
        public async Task<IActionResult> ExportStructuraReport(string reportName, CommitedOrdersBase commitedOrder)
        {
            try
            {
                using var reportStream = await _structuraReport.GenerateReport(commitedOrder, reportName, null);
                await WriteStreamToResponse(reportStream, $"Transport-PV-{commitedOrder.NumeLocatie}.docx", wordType);

                return new EmptyResult();
            }
            catch (Exception ex)
            {
                logger.LogError(new EventId(69), ex, "ExportStructuraReport");
                return StatusCode(500, "An error occurred while exporting the report.");
            }
        }

        [HttpPost("pv-report-multiple/{reportName}/{transportId}")]
        public async Task<IActionResult> ExportStructuraReportMultiple(string reportName, CommitedOrdersBase[] commitedOrders, int transportId)
        {
            var groupedCommited = GetOrdersGrouped(commitedOrders, t => t.CodLocatie);
            var transport = await _transportRepository.GetTransport(transportId);

            if (groupedCommited.Count == 1)
            {
                var fName = await GenerateAndWriteReport(groupedCommited.First(), transportId, transport.DriverName);
                await WriteStreamToResponse(fName, $"Transport-PV-{groupedCommited.First().NumeLocatie}.docx", wordType);
                return new EmptyResult();
            }

            using Stream tempStream = TempFileHelper.CreateTempFile();
            using (var wordDoc = WordprocessingDocument.Create(tempStream, WordprocessingDocumentType.Document))
            {
                var part = wordDoc.AddMainDocumentPart();
                part.Document = new Document();
                part.Document.Append(new Body());
                var destStylesPart = part.AddNewPart<StyleDefinitionsPart>();
                destStylesPart.Styles = new Styles();
                destStylesPart.Styles.Save();
                try
                {
                    for (int i = 0; i < groupedCommited.Count; i++)
                    {

                        var fName = await GenerateAndWriteReport(groupedCommited[i], transportId, transport.DriverName);
                        await CloneDocument(fName, part, 1, i > 0);
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(new EventId(69), ex, "ExportStructuraReport");
                    return StatusCode(500, "An error occurred while exporting the report.");
                }
                part.Document.Save();
            }
            await WriteStreamToResponse(tempStream, @$"Transport-PV-{transportId}.docx", wordType);

            return new EmptyResult();
        }

        [HttpPost("merge-commited-orders")]
        public async Task<IActionResult> ExportDispozitii(string[] internalNumber)
        {
            internalNumber = [.. internalNumber.Order()];
            var items = await _commitedOrdersRepository.GetCommitedOrders(internalNumber.Select(int.Parse).ToArray());
            var synonimLocations = (await _keyLocationRepository.GetLocations())
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

        private string GenerateMd5ForComplaintEntries(ComplaintDocument document)
        {
            StringBuilder sb = new StringBuilder();
            foreach (var item in document.complaintEntries)
            {
                if (!string.IsNullOrWhiteSpace(item.RefPartitionKey) && !string.IsNullOrWhiteSpace(item.RefRowKey))
                    sb.Append($"{item.RefRowKey}{item.RefPartitionKey}");
            }
            sb.Append(document.LocationCode);
            sb.Append(document.complaintEntries.Count());
            return _cryptoService.GetMd5(sb.ToString());
        }

        private string GenerateMd5ForCommitedOrders(CommitedOrdersBase commited)
        {
            List<string> list = new List<string>();
            foreach (var item in commited.Entry)
            {
                list.Add(_cryptoService.GetMd5($"{item.CodProdus}{item.NumarComanda}{item.Cantitate}"));
            }
            if (commited.NumarAviz.HasValue)
                list.Add(_cryptoService.GetMd5(commited.NumarAviz.ToString()!));

            list.Add(commited.CodLocatie);
            var stringToHash = string.Join("", list.Order());
            return _cryptoService.GetMd5(stringToHash);
        }

        private async Task CloneDocument(string fNameSource, MainDocumentPart target, int duplicates, bool skipFirstpageBreak = false)
        {
            using var savedStream = _storageService.Access(fNameSource, out var contentType);
            await CloneDocument(savedStream, target, duplicates, skipFirstpageBreak);
        }

        private async Task CloneDocument(Stream stream, MainDocumentPart target, int duplicates, bool skipFirstpageBreak = false)
        {
            int dupes = duplicates;
            using var docToClone = WordprocessingDocument.Open(stream, false);
            do
            {
                if (skipFirstpageBreak || duplicates > dupes) DocXServiceHelper.AddPageBreak(target);
                DocXServiceHelper.CloneBody(docToClone.MainDocumentPart!, target, _cryptoService.GetMd5);
            } while (--dupes > 0);
        }

        private async Task WriteStreamToResponse(string filePath, string fName, string contentType)
        {
            using var savedStream = _storageService.Access(filePath, out var contentType2);
            await WriteStreamToResponse(savedStream, fName, contentType2 ?? contentType);
        }

        private async Task WriteStreamToResponse(Stream stream, string fName, string contentType)
        {
            try
            {
                if (stream.CanSeek && stream.Position > 0)
                    stream.Position = 0;
                Response.Headers["Content-Disposition"] = $@"attachment; filename={fName}";
                Response.ContentType = contentType;
                await stream.CopyToAsync(Response.BodyWriter.AsStream());
            }
            finally
            {
                stream.Close();
            }
        }

        private List<CommitedOrdersBase> GetOrdersGrouped(CommitedOrdersBase[] commitedOrders, Func<CommitedOrdersBase, string> groupBy)
        {
            var groups = commitedOrders.GroupBy(groupBy).ToList();
            List<CommitedOrdersBase> result = new();

            for (int i = 0; i < groups.Count; i++)
            {
                var commited = groups[i];
                var sample = commited.First();
                sample.Entry.AddRange(commited.Skip(1).SelectMany(t => t.Entry));
                sample.Entry = [.. sample.Entry.GroupBy(x => x.CodProdus).Select(x => new CommitedOrderModel() {
                                        CodProdus = x.Key,
                                        Cantitate = x.Sum(t => t.Cantitate),
                                        NumeProdus = x.First().NumeProdus,
                                        NumarIntern = string.Join(";", x.Select(t => t.NumarIntern).Order()),
                                        NumarComanda = string.Join(";", x.Select(t => t.NumarComanda).Order())
                                    }).OrderBy(t => t.CodProdus)];
                result.Add(sample);
            }

            return result;
        }

        private async Task<string> GenerateAndWriteReport<T>(T item, int transportId, string driverName)
        {
            string metaDataKey = string.Empty;
            string fName = string.Empty;
            string md5 = string.Empty;
            Stream reportStream = Stream.Null;
            try
            {
                if (item is CommitedOrdersBase commited)
                {
                    md5 = GenerateMd5ForCommitedOrders(commited);
                    fName = $"transport/{transportId}/{commited.NumeLocatie}.docx";
                    metaDataKey = fName;
                }
                else if (item is ComplaintDocument complaint)
                {
                    md5 = GenerateMd5ForComplaintEntries(complaint);
                    fName = $"transport/{transportId}/Reclamatie-{complaint.LocationName}.docx";
                    metaDataKey = fName;
                }
                var metaData = await _metadataService.GetMetadata(metaDataKey);

                if (!metaData.ContainsKey("md5") || metaData["md5"] != md5)
                {
                    // should handle more generic Reports
                    if (item is ComplaintDocument complaint)
                    {
                        reportStream = await _reclamatiiReport.GenerateReport(complaint, driverName);
                        metaData["locationCode"] = complaint.LocationCode;
                    }
                    else if (item is CommitedOrdersBase c)
                    {
                        reportStream = await _structuraReport.GenerateReport(c, "Accesorii", driverName);
                        if (!string.IsNullOrWhiteSpace(c.NumarAviz?.ToString()))
                        {
                            metaData["aviz"] = c.NumarAviz.ToString()!;
                        }
                        metaData["locationCode"] = c.CodLocatie;
                        metaData["numarintern"] = string.Join(";", c.Entry.Select(t => t.NumarIntern).Distinct());
                    }

                    await _storageService.WriteTo(fName, reportStream, true);
                    metaData["transportId"] = transportId.ToString();
                    metaData["md5"] = md5;

                    await _metadataService.SetMetadata(metaDataKey, null, metaData);
                }
            }
            finally
            {
                if (reportStream != Stream.Null)
                    reportStream.Close();
            }

            return fName;
        }
    }
}
