using AutoMapper;
using AzureServices;
using DocumentFormat.OpenXml.Packaging;
using EntityDto.Transports;
using Microsoft.AspNetCore.Mvc;
using RepositoryContract;
using RepositoryContract.ExternalReferenceGroup;
using RepositoryContract.Transports;
using ServiceImplementation;
using ServiceInterface.Storage;
using WebApi.Models;
using WebApi.Services;
using WordDocument.Services;
using WordDocumentServices;

namespace WebApi.Controllers
{
    public class DocumentController : WebApiController2
    {
        private readonly IStorageService _storageService;
        private readonly ITransportRepository _transportRepository;
        private readonly ICryptoService _cryptoService;
        private readonly IExternalReferenceGroupRepository _externalReferenceGroupRepository;
        private readonly StructuraReport _structuraReport;
        private readonly SimpleReport _simpleReport;

        public DocumentController(ILogger<DocumentController> logger,
         BlobAccessStorageService storageService,
         ICryptoService cryptoService,
         IExternalReferenceGroupRepository externalReferenceGroupRepository,
         ITemplateDocumentWriter templateDocumentWriter,
         ITransportRepository transportRepository,
         StructuraReport structuraReport,
         SimpleReport simpleReport,
         ConnectionSettings connectionSettings,
         IMapper mapper) : base(logger, mapper)
        {
            _storageService = storageService;
            _transportRepository = transportRepository;
            _cryptoService = cryptoService;
            _externalReferenceGroupRepository = externalReferenceGroupRepository;
            _structuraReport = structuraReport;
            _simpleReport = simpleReport;
        }

        [HttpGet("transport-papers/{transportId}")]
        public async Task<IActionResult> GenerateTransportPapers(int transportId)
        {
            var externalRefs = await _externalReferenceGroupRepository.GetExternalReferences(@$"Id = {transportId} AND TableName = 'Transport' AND Ref_count > 0");
            Stream tempStream = TempFileHelper.CreateTempFile();

            var wordDoc = await DocXServiceHelper.CreateEmptyDoc(tempStream);

            for (int i = 0; i < externalRefs.Count; i++)
            {
                await CloneDocument(externalRefs[i].ExternalGroupId, wordDoc.MainDocumentPart!, 1, i > 0);
            }
            wordDoc.MainDocumentPart!.Document.Save();
            await WriteStreamToResponse(tempStream, @$"{transportId}.docx", octetStream);
            wordDoc.Dispose();
            return new EmptyResult();
        }

        [HttpPost("reclamatii-multiple/{transportId}")]
        public async Task<IActionResult> ExportReclamatiiMultiple(ComplaintDocument[] document, int transportId)
        {
            var groupedCommited = GetOrdersGrouped(document, t => t.LocationCode);
            var transport = await _transportRepository.GetTransport(transportId);

            if (groupedCommited.Count == 1)
            {
                var fName = await GenerateAndWriteReport(groupedCommited.First(), transport, transport.DriverName, t => t.LocationCode);
                await _transportRepository.UpdateTransport(transport, []);
                await WriteStreamToResponse(fName, $"Transport-PV-{groupedCommited.First().LocationName}.docx", wordType);
                return new EmptyResult();
            }

            using Stream tempStream = TempFileHelper.CreateTempFile();
            using (var wordDoc = await DocXServiceHelper.CreateEmptyDoc(tempStream))
            {
                try
                {
                    for (int i = 0; i < groupedCommited.Count; i++)
                    {
                        var fName = await GenerateAndWriteReport(groupedCommited[i], transport, transport.DriverName, t => t.LocationCode);
                        await CloneDocument(fName, wordDoc.MainDocumentPart!, 1, i > 0);
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(new EventId(69), ex, "ExportStructuraReport");
                    return StatusCode(500, "An error occurred while exporting the report.");
                }
                wordDoc.MainDocumentPart!.Document.Save();
            }
            await _transportRepository.UpdateTransport(transport, []);
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
                var fName = await GenerateAndWriteReport(groupedCommited.First(), transport, transport.DriverName, t => t.CodLocatie);
                await _transportRepository.UpdateTransport(transport, []);
                await WriteStreamToResponse(fName, $"Transport-PV-{groupedCommited.First().NumeLocatie}.docx", wordType);
                return new EmptyResult();
            }

            using Stream tempStream = TempFileHelper.CreateTempFile();
            using (var wordDoc = await DocXServiceHelper.CreateEmptyDoc(tempStream))
            {
                try
                {
                    for (int i = 0; i < groupedCommited.Count; i++)
                    {
                        var fName = await GenerateAndWriteReport(groupedCommited[i], transport, transport.DriverName, t => t.CodLocatie);
                        await CloneDocument(fName, wordDoc.MainDocumentPart!, 1, i > 0);
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(new EventId(69), ex, "ExportStructuraReport");
                    return StatusCode(500, "An error occurred while exporting the report.");
                }
                wordDoc.MainDocumentPart!.Document.Save();
            }
            await _transportRepository.UpdateTransport(transport, []);
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
                    var cOreders = commited.Cast<CommitedOrdersBase>().ToList();
                    sample.InternalNumbers = [.. cOreders.SelectMany(x => x.Entry.Select(y => y.NumarIntern)).Distinct()!];
                    sample.Entry.AddRange(cOreders.Skip(1).SelectMany(t => t.Entry));
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

        private async Task<string> GenerateAndWriteReport<T>(T item, TransportEntry transport, string driverName, Func<T, string> groupBy)
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
                    fName = $"transport/{transport.Id}/{SanitizeFileName(commited.NumeLocatie)}.docx";
                    metaDataKey = fName;
                }
                else if (item is ComplaintDocument complaint)
                {
                    md5 = complaint.GetMd5(_cryptoService.GetMd5);
                    fName = $"transport/{transport.Id}/Reclamatie-{SanitizeFileName(complaint.LocationName)}.docx";
                    metaDataKey = fName;
                }

                var externalItemMeta = new ExternalReferenceGroupEntry()
                {
                    Id = transport.Id,
                    TableName = nameof(Transport),
                    EntityType = nameof(Transport),
                    ExternalGroupId = fName,
                    PartitionKey = groupBy(item),
                    RowKey = md5
                };

                var oldEntity = (await _externalReferenceGroupRepository.UpsertExternalReferences([externalItemMeta])).FirstOrDefault();

                if (oldEntity == null)
                {
                    externalItemMeta = (await _externalReferenceGroupRepository.GetExternalReferences(
                        @$"TableName = '{nameof(Transport)}' AND PartitionKey = '{externalItemMeta.PartitionKey}' AND RowKey = '{externalItemMeta.RowKey}' AND Id = {transport.Id}"
                        )).First();
                }
                else
                {
                    externalItemMeta.G_Id = oldEntity.G_Id;
                }

                if (item is CommitedOrdersBase commited2)
                {
                    foreach (var transportItem in transport.TransportItems?.Where(x => x.DocumentType == 1 && commited2.InternalNumbers!.Contains(x.ExternalItemId)) ?? [])
                    {
                        transportItem.ExternalReferenceId = externalItemMeta.G_Id;
                    }
                }
                else if (item is ComplaintDocument complaint2)
                {
                    foreach (var transportItem in transport.TransportItems?.Where(x => x.DocumentType == 2 && complaint2.LocationCode == x.ExternalItemId2) ?? [])
                    {
                        transportItem.ExternalReferenceId = externalItemMeta.G_Id;
                    }
                }
                if (oldEntity == null || oldEntity.RowKey != md5 || !await _storageService.Exists(fName))
                {
                    var ctx = new Dictionary<string, object>() { { "driver_name", transport.DriverName } };
                    // should handle more generic Reports
                    if (item is ComplaintDocument complaint)
                    {
                        reportStream = await _simpleReport.GetSimpleReport("Reclamatii", complaint.LocationCode, complaint, ctx);
                    }
                    else if (item is CommitedOrdersBase c)
                    {
                        if (c.NumarAviz.HasValue)
                            ctx.Add("aviz_field", c.NumarAviz.Value);
                        reportStream = await _structuraReport.GenerateReport("Accesorii", c.CodLocatie, c, ctx);
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
