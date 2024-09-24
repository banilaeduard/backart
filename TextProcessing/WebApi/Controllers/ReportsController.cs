using AzureServices;
using DataAccess.Context;
using EntityDto;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Services.Storage;
using System.Linq;
using WorkSheetServices;

namespace WebApi.Controllers
{
    [Authorize(Roles = "admin")]
    public class ReportsController : WebApiController2
    {
        const string contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
        private CodeDbContext codeDbContext;
        private ImportsDbContext importsDbContext;
        private IStorageService storageService;
        private TableStorageService tableStorageService;

        public ReportsController(
            CodeDbContext codeDbContext,
            ImportsDbContext importsDbContext,
            ILogger<TicketController> logger,
            IStorageService storageService,
            TableStorageService tableStorageService) : base(logger)
        {
            this.codeDbContext = codeDbContext;
            this.importsDbContext = importsDbContext;
            this.storageService = storageService;
            this.tableStorageService = tableStorageService;
        }

        [HttpPost("MergeDispozitii")]
        public IActionResult MergeDispozitii()
        {
            List<DispozitieLivrare> items = new();
            string dateSuffix = DateTime.Now.ToString("ddMMyy");

            foreach (var file in Request.Form.Files)
            {
                using (var stream = file.OpenReadStream())
                {
                    var current = WorkbookReader.ReadWorkBook<DispozitieLivrare>(stream, 4);
                    items.AddRange(current);
                }
            }

            var keys = items.GroupBy(t => t.NumarIntern).Select(t => t.Key).ToDictionary(t => t);
            var resultName = string.Join("_", items.Select(t => t.NumeLocatie).Distinct().Take(3)) + "_" + Guid.NewGuid();
            var fileName = string.Format("DispozitiiMP_{0}/{1}.{2}", dateSuffix, resultName, "xlsx");

            foreach (var group in items.GroupBy(t => new { t.NumarIntern, t.CodProdus, t.CodLocatie }))
            {
                var sample = group.ElementAt(0);
                sample.Cantitate = group.Sum(t => t.Cantitate);

                if (keys.ContainsKey(sample.NumarIntern))
                {
                    var old_entries = tableStorageService.Query<DispozitieLivrareAzEntry>(t => sample.NumarIntern == t.NumarIntern);
                    keys.Remove(sample.NumarIntern);
                    tableStorageService.Delete(old_entries);
                }

                var az = DispozitieLivrareAzEntry.create(sample);
                az.PartitionKey = sample.CodProdus;
                az.RowKey = sample.NumarIntern;
                az.AggregatedFileNmae = fileName;
                tableStorageService.Insert(az);

                //keep product codes updated
                tableStorageService.Insert(new CodProduseAz
                {
                    PartitionKey = az.CodProdus,
                    RowKey = az.CodEan ?? "1",
                    CodeDisplay = az.NumeProdus,
                    Timestamp = DateTime.Now
                });
            }

            var reportData = WorkbookReportsService.GenerateReport(
                items,
                t => string.Format("{0} - {1}", t.NumarIntern.ToString(), t.NumeLocatie),
                t => string.Concat(t.CodProdus.AsSpan(0, 2), t.CodProdus.AsSpan(4, 1)),
                t => t.CodProdus);

            storageService.WriteTo(fileName, new BinaryData(reportData));
            return File(reportData, contentType, fileName);
        }

        [HttpPost("orderColete")]
        public async Task<IActionResult> OrderColete()
        {
            List<DispozitieLivrare> items = new();

            var dItems = importsDbContext.ComandaVanzare.ToList().Select(t => new DispozitieLivrare()
            {
                Cantitate = t.Cantitate,
                CodLocatie = t.CodLocatie,
                CodProdus = t.CodArticol,
                NumarIntern = t.NumarComanda,
                NumeProdus = t.NumeArticol
            });

            foreach (var structure in dItems.GroupBy(t => t.CodProdus))
            {
                var codes = codeDbContext.Codes
                    .Where(c => c.CodeValue == structure.Key)
                    .Include(t => t.Children)
                    .ThenInclude(t => t.Child)
                    .First();

                foreach (var disp in structure)
                {
                    if (codes.Children?.Count() > 0)
                        foreach (var code in codes.Children.Select(t => t.Child))
                        {
                            items.Add(new DispozitieLivrare()
                            {
                                CodProdus = code.CodeValue,
                                Cantitate = disp.Cantitate,
                                CodLocatie = disp.CodLocatie,
                                NumarIntern = disp.NumarIntern,
                                NumeProdus = code.CodeDisplay,
                                CodProdus2 = disp.CodProdus
                            });
                        }
                    else items.Add(disp);
                }
            }

            return File(WorkbookReportsService.GenerateReport(
                items.OrderBy(t => t.CodProdus2).ToList(),
                t => "dummy",
                t => t.CodProdus.AsSpan(0, 4).ToString(),
                t => t.CodProdus,
                "portrait"), contentType);
        }

        [HttpPost("MergeDispozitiiColete")]
        public async Task<IActionResult> MergeDispozitiiColete()
        {
            List<DispozitieLivrare> items = new();

            foreach (var file in Request.Form.Files)
            {
                using (var stream = file.OpenReadStream())
                {
                    var dItems = WorkbookReader.ReadWorkBook<DispozitieLivrare>(stream, 4);

                    foreach (var structure in dItems.GroupBy(t => t.CodProdus))
                    {
                        var code = codeDbContext.Codes
                            .Where(c => c.CodeValue == structure.Key)
                            .Include(t => t.Children)
                            .ThenInclude(t => t.Child)
                            .First();

                        if (string.IsNullOrEmpty(code.CodeValueFormat) && !string.IsNullOrEmpty(structure.ElementAt(0).CodEan)
                            || string.IsNullOrEmpty(code.AttributeTags) && !string.IsNullOrEmpty(structure.ElementAt(0).NumeCodificare)
                            )
                        {
                            code.CodeValueFormat = string.IsNullOrEmpty(code.CodeValueFormat) ? structure.ElementAt(0).CodEan : code.CodeValueFormat;
                            code.AttributeTags = structure.ElementAt(0).NumeCodificare;
                            await codeDbContext.SaveChangesAsync();
                        }

                        foreach (var disp in structure)
                        {
                            if (code.Children?.Count() > 0)
                                foreach (var code2 in code.Children.Select(t => t.Child))
                                {
                                    items.Add(new DispozitieLivrare()
                                    {
                                        CodProdus = code2.CodeValue,
                                        Cantitate = disp.Cantitate,
                                        CodLocatie = disp.CodLocatie,
                                        NumarIntern = disp.NumarIntern,
                                        NumeProdus = code2.CodeDisplay,
                                        CodProdus2 = disp.CodProdus
                                    });
                                }
                            else items.Add(disp);
                        }
                    }
                }
            }

            return File(WorkbookReportsService.GenerateReport(
                items.OrderBy(t => t.CodProdus2).ToList(),
                t => t.NumarIntern.ToString(),
                t => t.CodProdus.AsSpan(0, 4).ToString(),
                t => t.CodProdus,
                "portrait"), contentType);
        }
    }
}