using DataAccess.Context;
using DataAccess.Entities;
using EntityDto;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WorkSheetServices;

namespace WebApi.Controllers
{
    [Authorize(Roles = "admin")]
    public class UploadController : WebApiController2
    {
        private ImportsDbContext imports;
        private CodeDbContext codeDbContext;
        public UploadController(
            ILogger<UploadController> logger,
            ImportsDbContext imports,
            CodeDbContext codeDbContext
            ) : base(logger)
        {
            this.imports = imports;
            this.codeDbContext = codeDbContext;
        }

        [HttpPost("orders"), DisableRequestSizeLimit]
        public async Task<IActionResult> UploadOrders()
        {
            try
            {
                var file = Request.Form.Files[0];

                using (var stream = file.OpenReadStream())
                {
                    var items = WorkbookReader.ReadWorkBook<ComandaVanzare>(stream, 4);
                    var dbItems = items.Select(t => new ComandaVanzareEntry()
                    {
                        Cantitate = t.Cantitate,
                        CodArticol = t.CodArticol,
                        CodLocatie = t.CodLocatie,
                        DataDoc = t.DataDoc,
                        DetaliiDoc = t.DetaliiDoc,
                        DocId = t.DocId,
                        NumarComanda = t.NumarComanda,
                        NumeArticol = t.NumeArticol,
                        NumeLocatie = t.NumeLocatie,
                    }).ToList();

                    var codes = codeDbContext.Codes.AsNoTracking().ToList();
                    var toInsert = dbItems.Where(t => !codes.Any(x => x.CodeValue == t.CodArticol))
                                          .Select(t => new CodeLink() { CodeValue = t.CodArticol, CodeDisplay = t.NumeArticol, isRoot = true })
                                          .DistinctBy(t => t.CodeValue)
                                          .ToList();
                    codeDbContext.Codes.AddRange(toInsert);
                    await codeDbContext.SaveChangesAsync();

                    await imports.SetNewLocations(dbItems);
                    await imports.AddUniqueEntries(dbItems);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(new EventId(), ex, ex.Message);
                return BadRequest(ex.Message);
            }

            return Ok();
        }

        [HttpGet("orders")]
        public async Task<IActionResult> GetOrders()
        {
            return Ok(imports.ComandaVanzare.Select(GetOrderModel));
        }

        private ComandaVanzare GetOrderModel(ComandaVanzareEntry dbEntry)
        {
            return new ComandaVanzare()
            {
                Cantitate = dbEntry.Cantitate,
                CodArticol = dbEntry.CodArticol,
                CodLocatie = dbEntry.CodLocatie,
                DataDoc = dbEntry.DataDoc,
                DetaliiDoc = dbEntry.DetaliiDoc,
                DocId = dbEntry.DocId,
                NumarComanda = dbEntry.NumarComanda,
                NumeArticol = dbEntry.NumeArticol,
                NumeLocatie = dbEntry.NumeLocatie
            };
        }
    }
}
