using DataAccess.Context;
using EntityDto;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WorkSheetServices;

namespace WebApi.Controllers
{
    [Authorize(Roles = "admin")]
    public class ReportsController : WebApiController2
    {
        const string contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
        private CodeDbContext codeDbContext;
        private ImportsDbContext importsDbContext;

        public ReportsController(
            CodeDbContext codeDbContext,
            ImportsDbContext importsDbContext,
            ILogger<TicketController> logger) : base(logger)
        {
            this.codeDbContext = codeDbContext;
            this.importsDbContext = importsDbContext;
        }

        [HttpPost("MergeDispozitii")]
        public IActionResult MergeDispozitii()
        {
            List<DispozitieLivrare> items = new();

            foreach (var file in Request.Form.Files)
            {
                using (var stream = file.OpenReadStream())
                {
                    items.AddRange(WorkbookReader.ReadWorkBook<DispozitieLivrare>(stream, 4));
                }
            }

            return File(WorkbookReportsService.GenerateReport(
                items, 
                t => t.NumarIntern.ToString(), 
                t => string.Concat(t.CodProdus.AsSpan(0, 2), t.CodProdus.AsSpan(4, 1)), 
                t => t.CodProdus), contentType);
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
                t => t.CodLocatie,
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
                }
            }

            return File(WorkbookReportsService.GenerateReport(
                items.OrderBy(t => t.CodProdus2).ToList(), 
                t => t.NumarIntern.ToString(), 
                t => t.CodProdus.AsSpan(0,4).ToString(),
                t => t.CodProdus,
                "portrait"), contentType);
        }
    }
}