using DataAccess.Contexts;
using DataAccess.Entities;
using EntityDto;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WorkSheetServices;

namespace WebApi.Controllers
{
    [Authorize(Roles = "admin")]
    public class UploadController : WebApiController2
    {
        private ImportsDbContext imports;
        public UploadController(
            ILogger<UploadController> logger,
            ImportsDbContext imports
            ) : base(logger)
        {
            this.imports = imports;
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
                        NumeLocatie = t.NumeLocatie
                    }).ToList();

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
