using AutoMapper;
using EntityDto;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RepositoryContract.Orders;
using RepositoryContract.ProductCodes;
using WebApi.Models;
using WorkSheetServices;

namespace WebApi.Controllers
{
    [Authorize(Roles = "admin")]
    public class ReportsController : WebApiController2
    {
        const string contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
        private IOrdersRepository ordersRepository;
        private IProductCodeRepository productCodeRepository;

        public ReportsController(
            ILogger<ReportsController> logger,
            IProductCodeRepository productCodeRepository,
            IOrdersRepository ordersRepository,
            IMapper mapper) : base(logger, mapper)
        {
            this.productCodeRepository = productCodeRepository;
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

            var reportData = WorkbookReportsService.GenerateReport(
                items,
                t => string.Format("{0} - {1}", t.NumarIntern.ToString(), t.NumeLocatie),
                t => string.Concat(t.CodProdus.AsSpan(0, 2), t.CodProdus.AsSpan(4, 1)),
                t => t.CodProdus);

            return File(reportData, contentType);
        }

        [HttpPost("orderColete/{type}")]
        public async Task<IActionResult> OrderColete(int type, DispozitieModel[] orders)
        {
            List<DispozitieLivrare> items = new();

            List<DispozitieLivrare> dItems = [.. (orders.Select(x => new DispozitieLivrare {
                CodLocatie = x.CodLocatie,
                Cantitate = x.Cantitate,
                CodProdus = x.CodProdus,
                NumarIntern = x.NumarIntern.ToString(),
                NumeProdus = x.NumeProdus,
            }) ?? [])];

            if (!dItems.Any())
            {
                dItems.AddRange((await ordersRepository.GetOrders()).Select(t => new DispozitieLivrare()
                {
                    Cantitate = t.Cantitate,
                    CodLocatie = t.CodLocatie,
                    CodProdus = t.CodArticol,
                    NumarIntern = t.NumarComanda,
                    NumeProdus = t.NumeArticol
                }));
            }

            if (type == 2)
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
            else items.AddRange(dItems);

            return Ok(items);
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