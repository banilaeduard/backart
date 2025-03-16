using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using RepositoryContract.CommitedOrders;
using RepositoryContract.DataKeyLocation;
using RepositoryContract.Report;
using ServiceInterface.Storage;
using System.Text;
using WebApi.Models;
using WebApi.Services;

namespace WebApi.Controllers
{
    public class DocumentController : WebApiController2
    {
        const string wordType = "application/vnd.openxmlformats-officedocument.wordprocessingml.document";


        private IStorageService storageService;
        private IMetadataService metadataService;
        private IDataKeyLocationRepository keyLocationRepository;
        private ReclamatiiReport reclamatiiReport;
        private StructuraReport structuraReport;
        private IReportEntryRepository reportEntry;
        private ICommitedOrdersRepository commitedOrdersRepository;
        private ICryptoService cryptoService;

        public DocumentController(ILogger logger,
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
            this.storageService = storageService;
            this.metadataService = metadataService;
            this.keyLocationRepository = keyLocationRepository;
            this.reclamatiiReport = reclamatiiReport;
            this.structuraReport = structuraReport;
            this.reportEntry = reportEntry;
            this.commitedOrdersRepository = commitedOrdersRepository;
            this.cryptoService = cryptoService;
        }

        [HttpPost("reclamatii")]
        public async Task<IActionResult> ExportReclamatii(ComplaintDocument document)
        {
            try
            {
                StringBuilder sb = new StringBuilder();
                var locMap = (await reportEntry.GetLocationMapPathEntry("1", t => t.RowKey == document.LocationCode)).FirstOrDefault();

                if (locMap == null)
                {
                    locMap = await reportEntry.AddEntry(new LocationMapEntry()
                    {
                        PartitionKey = "1",
                        RowKey = document.LocationCode,
                        Folder = document.LocationName,
                        Location = document.LocationName
                    }, $"{nameof(LocationMapEntry)}");
                }

                foreach (var item in document.complaintEntries)
                {
                    if (!string.IsNullOrWhiteSpace(item.RefPartitionKey) && !string.IsNullOrWhiteSpace(item.RefRowKey))
                        sb.Append($"{item.RefRowKey}{item.RefPartitionKey}");
                }
                sb.Append(document.complaintEntries.Count());
                var md5 = cryptoService.GetMd5(sb.ToString());

                using (var leaseObj = await metadataService.GetLease($"locks/{document.NumarIntern}REC"))
                {
                    await leaseObj.Acquire(TimeSpan.FromMinutes(1));

                    var fName = $"reclamatii-drafts/{locMap.Folder}/{document.NumarIntern}.docx";
                    var metaData = await metadataService.GetMetadata(fName);

                    if (metaData.ContainsKey("md5"))
                    {
                        if (md5.Equals(metaData["md5"]))
                        {
                            var content = storageService.AccessIfExists(fName, out var contentType2);
                            return File(content, wordType);
                        }
                    }

                    var reportBytes = await reclamatiiReport.GenerateReport(document);
                    await storageService.WriteTo(fName, new BinaryData(reportBytes), true);
                    metaData["json"] = JsonConvert.SerializeObject(document);
                    metaData["md5"] = md5;
                    await metadataService.SetMetadata(fName, null, metaData);

                    return File(reportBytes, wordType);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(new EventId(69), ex, ex.Message);
                return File(await reclamatiiReport.GenerateReport(document), wordType);
            }
        }

        [HttpPost("structurareport/{reportName}/{dispozitie}")]
        public async Task<IActionResult> ExportStructuraReport(string reportName, int dispozitie)
        {
            var items = (await commitedOrdersRepository.GetCommitedOrder(dispozitie)).ToList();
            try
            {
                var locMap = (await reportEntry.GetLocationMapPathEntry("1", t => t.RowKey == items[0].CodLocatie)).FirstOrDefault();
                if (locMap == null)
                {
                    locMap = await reportEntry.AddEntry(new LocationMapEntry()
                    {
                        PartitionKey = "1",
                        RowKey = items[0].CodLocatie,
                        Folder = items[0].NumeLocatie,
                        Location = items[0].NumeLocatie
                    }, $"{nameof(LocationMapEntry)}");
                }

                var fName = $"pv_accesorii/{locMap.Folder}/{dispozitie}.docx";
                var metaData = await metadataService.GetMetadata(fName);

                List<string> list = new List<string>();
                foreach (var item in items)
                {
                    list.Add(cryptoService.GetMd5($"{item.CodProdus}{item.NumarComanda}{item.Cantitate}"));
                }
                if (items[0].NumarAviz.HasValue)
                    list.Add(cryptoService.GetMd5(items[0].NumarAviz!.ToString()));

                var stringToHash = string.Join("", list.Order());
                var md5 = cryptoService.GetMd5(stringToHash);

                using (var leaseObj = await metadataService.GetLease($"locks/{dispozitie}PV"))
                {
                    await leaseObj.Acquire(TimeSpan.FromMinutes(1));

                    if (metaData.ContainsKey("md5"))
                    {
                        if (md5.Equals(metaData["md5"]))
                        {
                            var content = storageService.AccessIfExists(fName, out var contentType2);
                            return File(content, wordType);
                        }
                    }

                    var reportBytes = await structuraReport.GenerateReport(items, reportName);
                    await storageService.WriteTo(fName, new BinaryData(reportBytes), true);
                    if (!string.IsNullOrWhiteSpace(items[0].NumarAviz?.ToString()))
                        metaData["aviz"] = items[0].NumarAviz?.ToString();
                    metaData["md5"] = md5;
                    await metadataService.SetMetadata(fName, null, metaData);

                    return File(reportBytes, wordType);

                }
            }
            catch (Exception ex)
            {
                return File(await structuraReport.GenerateReport(items, reportName), wordType);
            }
        }
    }
}
