using EntityDto;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RepositoryContract.CommitedOrders;
using RepositoryContract.Orders;
using RepositoryContract.ProductCodes;
using Services.Storage;
using WorkSheetServices;

namespace WebApi.Controllers
{
    [Authorize(Roles = "admin")]
    public class ReportsController : WebApiController2
    {
        const string contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
        private IStorageService storageService;
        private ICommitedOrdersRepository commitedOrdersRepository;
        private IOrdersRepository ordersRepository;
        private IProductCodeRepository productCodeRepository;

        public ReportsController(
            ILogger<ReportsController> logger,
            IStorageService storageService,
            ICommitedOrdersRepository commitedOrdersRepository,
            IProductCodeRepository productCodeRepository,
            IOrdersRepository ordersRepository) : base(logger)
        {
            this.productCodeRepository = productCodeRepository;
            this.storageService = storageService;
            this.commitedOrdersRepository = commitedOrdersRepository;
            this.ordersRepository = ordersRepository;
        }

        [HttpPost("MergeDispozitii")]
        public async Task<IActionResult> MergeDispozitii()
        {
            List<DispozitieLivrare> items = new();

            foreach (var file in Request.Form.Files)
            {
                using (var stream = file.OpenReadStream())
                {
                    var current = WorkbookReader.ReadWorkBook<DispozitieLivrare>(stream, 4);
                    items.AddRange(current);
                }
            }

            //var keys = items.GroupBy(t => t.NumarIntern).Select(t => t.Key).ToDictionary(t => t);
            var fileName = string.Format("DispozitiiMP_{0}/{1}_{2}.{3}", DateTime.Now.ToString("ddMMyy"),
                string.Join("_", items.Select(t => t.NumeLocatie).Distinct().Take(3)),
                DateTime.Now.ToString("HHmm"),
                "xlsx");

            //foreach (var group in items.GroupBy(t => new { t.NumarIntern, t.CodProdus, t.CodLocatie }))
            //{
            //    var sample = group.ElementAt(0);

            //    if (keys.ContainsKey(sample.NumarIntern))
            //    {
            //        var old_entries = await commitedOrdersRepository.GetCommitedOrders(t => sample.NumarIntern == t.PartitionKey);
            //        keys.Remove(sample.NumarIntern);
            //        await commitedOrdersRepository.DeleteCommitedOrders(old_entries.ToList());
            //    }

            //    var az = DispozitieLivrareEntry.create(sample, group.Sum(t => t.Cantitate));
            //    az.AggregatedFileNmae = fileName;
            //    await commitedOrdersRepository.InsertCommitedOrder(az);
            //}

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

            var dItems = (await ordersRepository.GetOrders()).Select(t => new DispozitieLivrare()
            {
                Cantitate = t.Cantitate,
                CodLocatie = t.CodLocatie,
                CodProdus = t.CodArticol,
                NumarIntern = t.NumarComanda,
                NumeProdus = t.NumeArticol
            });

            foreach (var structure in dItems.GroupBy(t => t.CodProdus))
            {
                var codes = await productCodeRepository.GetProductCodes(t => t.RootCode == structure.Key && t.Level == 3);

                foreach (var disp in structure)
                {
                    if (codes?.Count > 0)
                        foreach (var code in codes)
                        {
                            items.Add(new DispozitieLivrare()
                            {
                                CodProdus = code.Code,
                                Cantitate = disp.Cantitate,
                                CodLocatie = disp.CodLocatie,
                                NumarIntern = disp.NumarIntern,
                                NumeProdus = code.Name,
                                StatusName = disp.CodProdus
                            });
                        }
                    else items.Add(disp);
                }
            }

            return File(WorkbookReportsService.GenerateReport(
                items.OrderBy(t => t.StatusName).ToList(),
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
                        var codes = await productCodeRepository.GetProductCodes(t => t.RootCode == structure.Key.Trim() && t.Level == 3);

                        foreach (var disp in structure)
                        {
                            if (codes.Count() > 0)
                                foreach (var code2 in codes)
                                {
                                    items.Add(new DispozitieLivrare()
                                    {
                                        CodProdus = code2.Code,
                                        Cantitate = disp.Cantitate,
                                        CodLocatie = disp.CodLocatie,
                                        NumarIntern = disp.NumarIntern,
                                        NumeProdus = code2.Name,
                                        StatusName = disp.CodProdus
                                    });
                                }
                            else items.Add(disp);
                        }
                    }
                }
            }

            return File(WorkbookReportsService.GenerateReport(
                items.OrderBy(t => t.StatusName).ToList(),
                t => t.NumarIntern.ToString(),
                t => t.CodProdus.AsSpan(0, 4).ToString(),
                t => t.CodProdus,
                "portrait"), contentType);
        }
    }
}