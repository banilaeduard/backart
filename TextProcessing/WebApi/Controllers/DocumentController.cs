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
        private readonly IReportEntryRepository _reportEntry;
        private readonly ICommitedOrdersRepository _commitedOrdersRepository;
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
         IMapper mapper) : base(logger, mapper)
        {
            _storageService = storageService;
            _metadataService = storageService;
            _keyLocationRepository = keyLocationRepository;
            _reclamatiiReport = reclamatiiReport;
            _structuraReport = structuraReport;
            _reportEntry = reportEntry;
            _commitedOrdersRepository = commitedOrdersRepository;
            _cryptoService = cryptoService;
        }

        [HttpPost("transport-papers")]
        public async Task<IActionResult> GenerateTransportPapers(TransportPapersModel transportPapers)
        {
            Stream tempStream = TempFileHelper.CreateTempFile();
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
            Response.Headers["Content-Disposition"] = $@"attachment; filename=Transport-{transportPapers.TransportId}.docx";
            Response.ContentType = "application/octet-stream";
            var responseStream = Response.BodyWriter.AsStream();
            tempStream.Position = 0;
            await tempStream.CopyToAsync(responseStream);
            tempStream.Close();
            return new EmptyResult();
        }

        [HttpPost("reclamatii")]
        public async Task<IActionResult> ExportReclamatii(ComplaintDocument document)
        {
            try
            {
                using var reportStream = await _reclamatiiReport.GenerateReport(document, null);
                Response.Headers["Content-Disposition"] = $@"attachment; filename=Reclamatie-{document.LocationName}.docx";
                Response.ContentType = wordType;
                await reportStream.CopyToAsync(Response.BodyWriter.AsStream());

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
                await using var reportStream = await _structuraReport.GenerateReport(commitedOrder, reportName, null);
                Response.Headers["Content-Disposition"] = $@"attachment; filename={commitedOrder.NumeLocatie}.docx";
                Response.ContentType = wordType;
                await reportStream.CopyToAsync(Response.BodyWriter.AsStream());

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
            var items = await _commitedOrdersRepository.GetCommitedOrders(internalNumber.Select(int.Parse).ToArray());
            var synonimLocations = (await _keyLocationRepository.GetLocations())
                .Where(t => t.MainLocation && !string.IsNullOrWhiteSpace(t.ShortName) && items.Any(o => o.CodLocatie == t.LocationCode))
                .DistinctBy(t => t.LocationCode)
                .ToDictionary(x => x.LocationCode, x => x.ShortName);

            var missing = internalNumber.Except(items.DistinctBy(t => t.NumarIntern).Select(t => t.NumarIntern));

            if (missing.Any()) return NotFound(string.Join(", ", missing));

            Response.Headers["Content-Disposition"] = $@"attachment; filename={string.Join("_", internalNumber.Take(5))}.xlsx";
            Response.ContentType = "application/octet-stream";
            //Response.Headers["Transfer-Encoding"] = "chunked";

            // Use AsStream() to get a writeable stream for the response body
            await using var responseStream = Response.BodyWriter.AsStream();
            await WorkbookReportsService.GenerateReport(
                               items.Cast<CommitedOrder>().ToList(),
                               t => synonimLocations.ContainsKey(t.CodLocatie) ? synonimLocations[t.CodLocatie] : t.CodLocatie.ToUpperInvariant(),
                               t => string.Concat(t.CodProdus.AsSpan(0, 2), t.CodProdus.AsSpan(4, 1)),
                               t => t.NumeProdus, responseStream);
            return new EmptyResult();
        }

        private async Task<LocationMapEntry> GetOrCreateLocationMapEntry(string locationCode, string locationName)
        {
            var locMap = (await _reportEntry.GetLocationMapPathEntry("1", t => t.RowKey == locationCode)).FirstOrDefault();
            if (locMap == null)
            {
                locMap = await _reportEntry.AddEntry(new LocationMapEntry()
                {
                    PartitionKey = "1",
                    RowKey = locationCode,
                    Folder = locationName,
                    Location = locationName
                }, $"{nameof(LocationMapEntry)}");
            }
            return locMap;
        }

        private string GenerateMd5ForComplaintEntries(ComplaintDocument document)
        {
            StringBuilder sb = new StringBuilder();
            foreach (var item in document.complaintEntries)
            {
                if (!string.IsNullOrWhiteSpace(item.RefPartitionKey) && !string.IsNullOrWhiteSpace(item.RefRowKey))
                    sb.Append($"{item.RefRowKey}{item.RefPartitionKey}");
            }
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

            var stringToHash = string.Join("", list.Order());
            return _cryptoService.GetMd5(stringToHash);
        }

        private async Task CloneDocument(string fNameSource, MainDocumentPart target, int duplicates, bool skipFirstpageBreak = false)
        {
            int dupes = duplicates;
            using var savedStream = _storageService.Access(fNameSource, out var contentType);
            using var docToClone = WordprocessingDocument.Open(savedStream, false);
            do
            {
                if (skipFirstpageBreak || duplicates > dupes) DocXServiceHelper.AddPageBreak(target);
                DocXServiceHelper.CloneBody(docToClone.MainDocumentPart!, target, _cryptoService.GetMd5);
            } while (--dupes > 0);
        }

        private async Task<string> GenerateAndWriteReport<T>(T item, int transportId, string driverName)
        {
            string metaDataKey = string.Empty;
            string fName = string.Empty;
            string md5 = string.Empty;
            Stream reportStream = Stream.Null;

            if (item is CommitedOrdersBase commited)
            {
                md5 = GenerateMd5ForCommitedOrders(commited);
                fName = $"transport/{transportId}/{commited.NumeLocatie}-{(commited.NumarAviz.HasValue ? commited.NumarAviz.Value.ToString() : "")}.docx";
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
                }
                else if (item is CommitedOrdersBase c)
                {
                    reportStream = await _structuraReport.GenerateReport(c, "Accesorii", driverName);
                }

                await _storageService.WriteTo(fName, reportStream, true);
                if (item is CommitedOrdersBase commited2 && !string.IsNullOrWhiteSpace(commited2.NumarAviz?.ToString()))
                {
                    metaData["aviz"] = commited2.NumarAviz.ToString()!;
                }
                metaData["md5"] = md5;
                await _metadataService.SetMetadata(metaDataKey, null, metaData);
            }

            return fName;
        }
    }
}
