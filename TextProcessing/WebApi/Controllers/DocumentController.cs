using AutoMapper;
using EntityDto.CommitedOrders;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using RepositoryContract.CommitedOrders;
using RepositoryContract.DataKeyLocation;
using RepositoryContract.Report;
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
        const string excelType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

        private readonly IStorageService _storageService;
        private readonly IMetadataService _metadataService;
        private readonly IDataKeyLocationRepository _keyLocationRepository;
        private readonly ReclamatiiReport _reclamatiiReport;
        private readonly StructuraReport _structuraReport;
        private readonly IReportEntryRepository _reportEntry;
        private readonly ICommitedOrdersRepository _commitedOrdersRepository;
        private readonly ICryptoService _cryptoService;

        public DocumentController(ILogger<DocumentController> logger,
         IStorageService storageService,
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
            _metadataService = metadataService;
            _keyLocationRepository = keyLocationRepository;
            _reclamatiiReport = reclamatiiReport;
            _structuraReport = structuraReport;
            _reportEntry = reportEntry;
            _commitedOrdersRepository = commitedOrdersRepository;
            _cryptoService = cryptoService;
        }

        [HttpPost("reclamatii")]
        public async Task<IActionResult> ExportReclamatii(ComplaintDocument document)
        {
            try
            {
                var locMap = await GetOrCreateLocationMapEntry(document.LocationCode, document.LocationName);
                var md5 = GenerateMd5ForComplaintEntries(document);

                var fName = $"reclamatii-drafts/{locMap.Folder}/{document.NumarIntern}.docx";
                var metaName = $"reclamatii-drafts_{_cryptoService.GetMd5(locMap.Folder)}_{document.NumarIntern}";
                var metaData = await _metadataService.GetMetadata(metaName);

                if (metaData.ContainsKey("md5") && md5.Equals(metaData["md5"]) && _storageService.AccessIfExists(fName, out var contentType2, out var content))
                {
                    return File(content, wordType);
                }

                var reportBytes = await _reclamatiiReport.GenerateReport(document);
                await _storageService.WriteTo(fName, new BinaryData(reportBytes), true);
                metaData["json"] = JsonConvert.SerializeObject(document);
                metaData["md5"] = md5;
                await _metadataService.SetMetadata(metaName, null, metaData);

                return File(reportBytes, wordType);
            }
            catch (Exception ex)
            {
                logger.LogError(new EventId(69), ex, "ExportReclamatii");
                return StatusCode(500, "An error occurred while exporting the report.");
            }
        }

        [HttpPost("pv-report/{reportName}")]
        public async Task<IActionResult> ExportStructuraReport(string reportName, int[] commitedOrders)
        {
            var items = await _commitedOrdersRepository.GetCommitedOrders(commitedOrders);
            try
            {
                var locMap = await GetOrCreateLocationMapEntry(items[0].CodLocatie, items[0].NumeLocatie);
                var md5 = GenerateMd5ForCommitedOrders(items);

                var name = string.Join("-", commitedOrders);
                var fName = $"pv_accesorii/{locMap.Folder}/{name}.docx";
                var metaName = $"pv_accesorii_{_cryptoService.GetMd5(locMap.Folder)}_{name}.docx";
                var metaData = await _metadataService.GetMetadata(metaName);

                if (metaData.ContainsKey("md5") && md5.Equals(metaData["md5"]) && _storageService.AccessIfExists(fName, out var contentType2, out var content))
                {
                    return File(content, wordType);
                }

                var reportBytes = await _structuraReport.GenerateReport(items, reportName);
                await _storageService.WriteTo(fName, new BinaryData(reportBytes), true);
                if (!string.IsNullOrWhiteSpace(items[0].NumarAviz?.ToString()))
                    metaData["aviz"] = items[0].NumarAviz?.ToString();
                metaData["md5"] = md5;
                await _metadataService.SetMetadata(metaName, null, metaData);

                return File(reportBytes, wordType);
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

            var reportData = WorkbookReportsService.GenerateReport(
                items.Cast<CommitedOrder>().ToList(),
                t => synonimLocations.ContainsKey(t.CodLocatie) ? synonimLocations[t.CodLocatie] : t.CodLocatie.ToUpperInvariant(),
                t => string.Concat(t.CodProdus.AsSpan(0, 2), t.CodProdus.AsSpan(4, 1)),
                t => t.NumeProdus);

            return File(reportData, excelType);
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

        private string GenerateMd5ForCommitedOrders(IEnumerable<CommitedOrder> items)
        {
            List<string> list = new List<string>();
            foreach (var item in items)
            {
                list.Add(_cryptoService.GetMd5($"{item.CodProdus}{item.NumarComanda}{item.Cantitate}"));
            }
            if (items.First().NumarAviz.HasValue)
                list.Add(_cryptoService.GetMd5(items.First().NumarAviz!.ToString()));

            var stringToHash = string.Join("", list.Order());
            return _cryptoService.GetMd5(stringToHash);
        }
    }
}
