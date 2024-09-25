using AzureServices;
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
        private TableStorageService tableStorageService;

        public UploadController(
            ILogger<UploadController> logger,
            ImportsDbContext imports,
            CodeDbContext codeDbContext,
            TableStorageService tableStorageService
            ) : base(logger)
        {
            this.imports = imports;
            this.codeDbContext = codeDbContext;
            this.tableStorageService = tableStorageService;
        }

        [HttpPost("orders"), DisableRequestSizeLimit]
        public async Task<IActionResult> UploadOrders()
        {
            var file = Request.Form.Files[0];

            using (var stream = file.OpenReadStream())
            {
                var items = WorkbookReader.ReadWorkBook<ComandaVanzare>(stream, 4);
                var newEntries = items.Select(ComandaVanzareAzEntry.create).GroupBy(ComandaVanzareAzEntry.PKey).ToDictionary(t => t.Key, MergeByHash);

                foreach (var item in newEntries)
                {
                    var oldEntries = tableStorageService.Query<ComandaVanzareAzEntry>(t => t.PartitionKey == item.Key).ToList();
                    var comparer = ComandaVanzareAzEntry.GetEqualityComparer();
                    var comparerQuantity = ComandaVanzareAzEntry.GetEqualityComparer(true);

                    var currentEntries = newEntries[item.Key];

                    var exceptAdd = currentEntries.Except(oldEntries, comparer).ToList();
                    var exceptDelete = oldEntries.Except(currentEntries, comparer).ToList();

                    var intersectOld2 = oldEntries.Intersect(currentEntries, comparer).ToList();
                    var intersectNew2 = currentEntries.Intersect(oldEntries, comparer).ToList();

                    var intersectNew = intersectNew2.Except(intersectOld2, comparerQuantity).ToList().ToDictionary(comparer.GetHashCode);
                    var intersectOld = intersectOld2.Except(intersectNew2, comparerQuantity).ToList().ToDictionary(comparer.GetHashCode);

                    foreach (var differential in intersectOld)
                    {
                        differential.Value.Cantitate = differential.Value.Cantitate - intersectNew[differential.Key].Cantitate;
                    }

                    await tableStorageService.PrepareUpsert(exceptDelete)
                                             .Concat(tableStorageService.PrepareUpsert(intersectOld.Select(t => t.Value)))
                                             .ExecuteBatch(ComandaVanzareAzEntry.GetProgressTableName());

                    await tableStorageService.PrepareUpsert(intersectNew.Values)
                                            .Concat(tableStorageService.PrepareInsert(exceptAdd))
                                            .Concat(tableStorageService.PrepareDelete(exceptDelete))
                                            .ExecuteBatch();
                }
            }

            return Ok();
        }

        [HttpGet("orders")]
        public async Task<IActionResult> GetOrders()
        {
            var codeLinks = codeDbContext.CodeLinkNode.Include(t => t.Parent).ToList();
            return Ok(tableStorageService.Query<ComandaVanzareAzEntry>(t => true).Select(GetOrderModel(codeLinks)));
        }

        private Func<ComandaVanzareAzEntry, ComandaVanzare> GetOrderModel(List<CodeLinkNode> codeLinkls)
        {
            return (ComandaVanzareAzEntry dbEntry) =>
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
                    NumeLocatie = dbEntry.NumeLocatie,
                    HasChildren = codeLinkls.Any(t => t.Parent.CodeValue == dbEntry.CodArticol) ? true : null,
                    NumePartener = dbEntry.NumePartener
                };
            };
        }

        private IEnumerable<ComandaVanzareAzEntry> MergeByHash(IEnumerable<ComandaVanzareAzEntry> list)
        {
            var comparer = ComandaVanzareAzEntry.GetEqualityComparer();
            foreach (var items in list.GroupBy(comparer.GetHashCode))
            {
                var sample = items.ElementAt(0);
                sample.Cantitate = items.Sum(t => t.Cantitate);
                if (items.Distinct(comparer).Count() > 1) throw new Exception("We fucked boyzs");
                yield return sample;
            }
        }
    }
}
