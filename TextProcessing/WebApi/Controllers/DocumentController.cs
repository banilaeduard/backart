using AutoMapper;
using AzureServices;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using EntityDto.Transports;
using Microsoft.AspNetCore.Mvc;
using RepositoryContract.ExternalReferenceGroup;
using RepositoryContract.Transports;
using ServiceImplementation;
using ServiceInterface.Storage;
using WebApi.Models;
using WebApi.Services;

namespace WebApi.Controllers
{
    public class DocumentController : WebApiController2
    {
        private readonly IStorageService _storageService;
        private readonly ReclamatiiReport _reclamatiiReport;
        private readonly StructuraReport _structuraReport;
        private readonly ITransportRepository _transportRepository;
        private readonly ICryptoService _cryptoService;
        private readonly IExternalReferenceGroupRepository _externalReferenceGroupRepository;

        public DocumentController(ILogger<DocumentController> logger,
         BlobAccessStorageService storageService,
         ReclamatiiReport reclamatiiReport,
         StructuraReport structuraReport,
         ICryptoService cryptoService,
         IExternalReferenceGroupRepository externalReferenceGroupRepository,
         ITransportRepository transportRepository,
         IMapper mapper) : base(logger, mapper)
        {
            _storageService = storageService;
            _reclamatiiReport = reclamatiiReport;
            _structuraReport = structuraReport;
            _transportRepository = transportRepository;
            _cryptoService = cryptoService;
            _externalReferenceGroupRepository = externalReferenceGroupRepository;
        }

        [HttpGet("transport-papers/{transportId}")]
        public async Task<IActionResult> GenerateTransportPapers(int transportId)
        {
            var externalRefs = await _externalReferenceGroupRepository.GetExternalReferences(@$"Id = {transportId} AND TableName = 'Transport'");
            Stream tempStream = TempFileHelper.CreateTempFile();

            var wordDoc = WordprocessingDocument.Create(tempStream, WordprocessingDocumentType.Document);
            var part = wordDoc.AddMainDocumentPart();
            part.Document = new Document();
            part.Document.Append(new Body());
            var destStylesPart = part.AddNewPart<StyleDefinitionsPart>();
            destStylesPart.Styles = new Styles();
            destStylesPart.Styles.Save();

            for (int i = 0; i < externalRefs.Count; i++)
            {
                await CloneDocument(externalRefs[i].ExternalGroupId, part, 1, i > 0);
            }
            wordDoc.Close();
            await WriteStreamToResponse(tempStream, @$"{transportId}.docx", octetStream);

            return new EmptyResult();
        }

        [HttpPost("reclamatii-multiple/{transportId}")]
        public async Task<IActionResult> ExportReclamatiiMultiple(ComplaintDocument[] document, int transportId)
        {
            var groupedCommited = GetOrdersGrouped(document, t => t.LocationCode);
            var transport = await _transportRepository.GetTransport(transportId);

            if (groupedCommited.Count == 1)
            {
                var fName = await GenerateAndWriteReport(groupedCommited.First(), transportId, transport.DriverName, t => t.LocationCode);
                await WriteStreamToResponse(fName, $"Transport-PV-{groupedCommited.First().LocationName}.docx", wordType);
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
                        var fName = await GenerateAndWriteReport(groupedCommited[i], transportId, transport.DriverName, t => t.LocationCode);
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
            await WriteStreamToResponse(tempStream, @$"Transport-REC_PV-{transportId}.docx", wordType);

            return new EmptyResult();
        }

        [HttpPost("pv-report-multiple/{reportName}/{transportId}")]
        public async Task<IActionResult> ExportStructuraReportMultiple(string reportName, CommitedOrdersBase[] commitedOrders, int transportId)
        {
            var groupedCommited = GetOrdersGrouped(commitedOrders, t => t.CodLocatie);
            var transport = await _transportRepository.GetTransport(transportId);

            if (groupedCommited.Count == 1)
            {
                var fName = await GenerateAndWriteReport(groupedCommited.First(), transportId, transport.DriverName, t => t.CodLocatie);
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

                        var fName = await GenerateAndWriteReport(groupedCommited[i], transportId, transport.DriverName, t => t.CodLocatie);
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

        private List<T> GetOrdersGrouped<T>(T[] commitedOrders, Func<T, string> groupBy)
        {
            var groups = commitedOrders.GroupBy(groupBy).ToList();
            List<T> result = new();

            for (int i = 0; i < groups.Count; i++)
            {
                var commited = groups[i];
                result.Add(commited.First());
                if (commited.First() is CommitedOrdersBase sample)
                {
                    sample.Entry.AddRange(commited.Skip(1).Cast<CommitedOrdersBase>().SelectMany(t => t.Entry));
                    sample.Entry = [.. sample.Entry.GroupBy(x => x.CodProdus).Select(x => new CommitedOrderModel() {
                                        CodProdus = x.Key,
                                        Cantitate = x.Sum(t => t.Cantitate),
                                        NumeProdus = x.First().NumeProdus,
                                        NumarIntern = string.Join(";", x.Select(t => t.NumarIntern).Order()),
                                        NumarComanda = string.Join(";", x.Select(t => t.NumarComanda).Order())
                                    }).OrderBy(t => t.CodProdus)];
                }
                else if (commited.First() is ComplaintDocument cDoc)
                {
                    cDoc.complaintEntries.AddRange(commited.Skip(1).Cast<ComplaintDocument>().SelectMany(t => t.complaintEntries));
                }
            }

            return result;
        }

        private async Task<string> GenerateAndWriteReport<T>(T item, int transportId, string driverName, Func<T, string> groupBy)
        {
            string metaDataKey = string.Empty;
            string fName = string.Empty;
            string md5 = string.Empty;
            Stream reportStream = Stream.Null;

            try
            {
                if (item is CommitedOrdersBase commited)
                {
                    md5 = commited.GetMd5(_cryptoService.GetMd5);
                    fName = $"transport/{transportId}/{SanitizeFileName(commited.NumeLocatie)}.docx";
                    metaDataKey = fName;
                }
                else if (item is ComplaintDocument complaint)
                {
                    md5 = complaint.GetMd5(_cryptoService.GetMd5);
                    fName = $"transport/{transportId}/Reclamatie-{SanitizeFileName(complaint.LocationName)}.docx";
                    metaDataKey = fName;
                }

                var externalItemMeta = new ExternalReferenceGroupEntry()
                {
                    Id = transportId,
                    TableName = nameof(Transport),
                    EntityType = nameof(Transport),
                    ExternalGroupId = fName,
                    PartitionKey = groupBy(item),
                    RowKey = md5
                };

                var oldEntyry = await _externalReferenceGroupRepository.UpsertExternalReferences([externalItemMeta]);

                if (oldEntyry.Count == 0 || oldEntyry[0].RowKey != md5 || !await _storageService.Exists(fName))
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
                }
            }
            finally
            {
                if (reportStream != Stream.Null)
                    reportStream.Close();
            }

            return fName;
        }

        private async Task WriteStreamToResponse(string filePath, string fName, string contentType)
        {
            using var savedStream = _storageService.Access(filePath, out var contentType2);
            await WriteStreamToResponse(savedStream, fName, contentType2 ?? contentType);
        }

        private async Task CloneDocument(string fNameSource, MainDocumentPart target, int duplicates, bool skipFirstpageBreak = false)
        {
            using var savedStream = _storageService.Access(fNameSource, out var contentType);
            await DocXServiceHelper.CloneDocument(savedStream, target, duplicates, _cryptoService.GetMd5, skipFirstpageBreak);
        }
    }
}
