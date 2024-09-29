namespace WebApi.Controllers
{
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Authorization;

    using Microsoft.Extensions.Logging;


    using DataAccess.Entities;
    using System.Linq;
    using Microsoft.EntityFrameworkCore;
    using global::WebApi.Models;
    using RepositoryContract.ProductCodes;
    using AzureSerRepositoryContract.ProductCodesvices;
    using DataAccess.Context;

    public class CodesController : WebApiController2
    {
        private IProductCodeRepository productCodeRepository;
        private ImportsDbContext imports;
        public CodesController(
            IProductCodeRepository productCodeRepository,
            ImportsDbContext imports,
            ILogger<CodesController> logger) : base(logger)
        {
            this.productCodeRepository = productCodeRepository;
            this.imports = imports;
        }

        [HttpGet]
        [Authorize(Roles = "partener, admin")]
        public async Task<IActionResult> GetCodes()
        {
            CodeLinkModel map(ProductCodeEntry t)
            {
                return new CodeLinkModel()
                {
                    CodeDisplay = t.Name,
                    CodeValue = t.Code,
                    Level = t.Level,
                    CodeBar = t.Bar,
                    ParentCode = t.ParentCode,
                    RootCode = t.RootCode
                    //barcode = BarCodeGenerator.GenerateDataUrlBarCode(t.CodeValue)
                };
            }
            //var x = this.imports.ComandaVanzare.FromSql
            //    (@$"SELECT
	           //    s.statusname
	           //    ,i.docnumber DocId
            //      ,i.externalnumber 
	           //   ,il.itemname NumeArticol
	           //   ,il.itemstatusid
	           //   ,il.calcquantity
	           //   ,il.docquantity Cantitate
	           //   ,il2.itemname itemname2
	           //   ,il2.itemstatusid itemstatusid2
	           //   ,il2.calcquantity calcquantity2
	           //   ,il2.docquantity docquantity2
	           //   ,i2.externalnumber NumarComanda
            //  FROM [tehninvest_tfs].[dbo].[inventory] i
            //  join dbo.doctype d on d.objectid = i.doctypeid
            //  join dbo.[status] s on i.statusid = s.objectid
            //  join dbo.inventoryline il on il.inventoryid = i.objectid
            //  join dbo.itemstatus s2 on il.itemstatusid = s2.objectid
            //  join dbo.inventoryline il2 on il.inventorylineid2 = il2.objectid
            //  join dbo.inventory i2 on il2.inventoryid = i2.objectid
            //  where d.doctypekey = '18' and i2.docnumber > 16000").Take(100).ToList();
            var result = await productCodeRepository.GetProductCodes();

            return Ok(result.Select(map));
        }

        //[HttpGet("attributes")]
        //[Authorize(Roles = "partener, admin")]
        //public IActionResult GetCodesAttributes()
        //{
        //    return Ok(this.codeDbContext.CodeAttribute);
        //}

        //[HttpPost]
        //[Authorize(Roles = "admin")]
        //public async Task<IActionResult> SaveCodes(List<CodeLinkModel> codes)
        //{
        //    var codeDb = codeDbContext.Codes.FirstOrDefault(t => codes[0].CodeValue == t.CodeValue);
        //    if (codeDb != null) return BadRequest("Code already exists");

        //    var code = codes[0];

        //    codeDb = new CodeLink()
        //    {
        //        isRoot = true,
        //        Children = []
        //    };
        //    codeDb = codeDbContext.Add(codeDb).Entity;

        //    await MapCode(code, codeDb);

        //    await codeDbContext.SaveChangesAsync();
        //    return Ok(codes);
        //}

        //[HttpPatch]
        //[Authorize(Roles = "admin")]
        //public async Task<IActionResult> UpdateCodes(List<CodeLinkModel> codes)
        //{
        //    var codeDb = codeDbContext.Codes.Where(t => codes[0].Id == t.Id).Include(t => t.Children).ThenInclude(t => t.Child).ToList();

        //    foreach (var item in codes)
        //    {
        //        var dbEntry = codeDb.Where(t => t.Id == item.Id).First();
        //        await MapCode(item, dbEntry);
        //    }

        //    await codeDbContext.SaveChangesAsync();
        //    return Ok(codes);
        //}

        //[HttpPost("delete")]
        //[Authorize(Roles = "admin")]
        //public async Task<IActionResult> DeleteCodes(List<CodeLink> codes)
        //{
        //    this.codeDbContext.Codes.RemoveRange(codes);
        //    await this.codeDbContext.SaveChangesAsync();

        //    return Ok();
        //}

        //[HttpPost("attributes")]
        //[Authorize(Roles = "admin")]
        //public async Task<IActionResult> SaveCodesAttributes(List<CodeAttribute> codes)
        //{
        //    this.codeDbContext.AddRange(codes);
        //    await this.codeDbContext.SaveChangesAsync();

        //    return Ok(codes);
        //}

        //[HttpPatch("attributes")]
        //[Authorize(Roles = "admin")]
        //public async Task<IActionResult> UpdateCodesAttributes(List<CodeAttribute> codes)
        //{
        //    this.codeDbContext.UpdateRange(codes);
        //    await this.codeDbContext.SaveChangesAsync();

        //    return Ok(codes);
        //}

        //[HttpPost("attributes/delete")]
        //[Authorize(Roles = "admin")]
        //public async Task<IActionResult> DeleteCodesAttributes(List<CodeAttribute> codes)
        //{
        //    codes.ForEach(code =>
        //    this.codeDbContext.Entry(new CodeAttribute() { Tag = code.Tag, InnerValue = code.InnerValue })
        //                      .State = EntityState.Deleted);
        //    await this.codeDbContext.SaveChangesAsync();

        //    return Ok();
        //}

        //[HttpPost("upload"), DisableRequestSizeLimit]
        //[Authorize(Roles = "admin")]
        //public async Task<IActionResult> UploadStructure()
        //{

        //    var file = Request.Form.Files[0];

        //    using (var stream = file.OpenReadStream())
        //    {
        //        var items = WorkbookReader.ReadWorkBook<StructuraCod>(stream, 2);
        //        var existingRootCodes = codeDbContext.Codes.Where(t => t.isRoot && t.CodeValueFormat != null).AsNoTracking().ToDictionary(t => t.CodeValueFormat);

        //        foreach (var code in items.DistinctBy(t => t.CodEanProdusParinte))
        //        {
        //            if (!string.IsNullOrEmpty(code.CodEanProdusParinte) && !existingRootCodes.ContainsKey(code.CodEanProdusParinte))
        //            {
        //                codeDbContext.Codes.Add(new CodeLink() { CodeValue = code.NumeArticol, CodeDisplay = code.NumeArticol, CodeValueFormat = code.CodEanProdusParinte, isRoot = true });
        //            }
        //        }
        //        await codeDbContext.SaveChangesAsync();

        //        var existingRootCodes2 = codeDbContext.Codes.Where(t => t.isRoot).AsNoTracking().ToList();
        //        Dictionary<string, string> syn = new();

        //        foreach (var code in items.DistinctBy(t => t.NumeArticol))
        //        {
        //            var item = existingRootCodes2.Where(t => t.CodeDisplay == code.NumeArticol || t.CodeValueFormat == code.CodEanProdusParinte).FirstOrDefault();
        //            if (item != null)
        //            {
        //                if (item.CodeDisplay != code.NumeArticol)
        //                    syn.Add(code.NumeArticol, item.CodeDisplay);
        //            }
        //            else
        //            {
        //                codeDbContext.Codes.Add(new CodeLink() { CodeValue = code.NumeArticol, CodeDisplay = code.NumeArticol, CodeValueFormat = code.CodEanProdusParinte, isRoot = true });
        //                await codeDbContext.SaveChangesAsync();
        //            }
        //        }

        //        var childNodes = codeDbContext.Codes.Where(t => !t.isRoot).AsNoTracking().ToDictionary(t => t.CodeValue, t => t);

        //        foreach (var item in items.DistinctBy(t => t.CodColet))
        //        {
        //            if (!childNodes.ContainsKey(item.CodColet))
        //            {
        //                codeDbContext.Codes.Add(new CodeLink { CodeValue = item.CodColet, CodeDisplay = item.NumeColet, isRoot = false, CodeValueFormat = item.NumarColet });
        //            }
        //        }

        //        await codeDbContext.SaveChangesAsync();

        //        existingRootCodes = codeDbContext.Codes.Where(t => t.isRoot).Include(t => t.Children).ThenInclude(t => t.Child).ToDictionary(t => t.CodeDisplay, t => t);

        //        string getKey(Dictionary<string, string> syn, string key)
        //        {
        //            if (syn.ContainsKey(key)) return syn[key];
        //            return key;
        //        }

        //        foreach (var missing in items.Where(t => existingRootCodes[getKey(syn, t.NumeArticol)].Children.FirstOrDefault(x => x.Child.CodeValue == t.CodColet) == null))
        //        {
        //            existingRootCodes[getKey(syn, missing.NumeArticol)].Children.Add(new CodeLinkNode()
        //            {
        //                Child = codeDbContext.Codes.First(t => t.CodeValue == missing.CodColet),
        //                Parent = existingRootCodes[getKey(syn, missing.NumeArticol)]
        //            });
        //        }

        //        await codeDbContext.SaveChangesAsync();
        //    }

        //    return Ok();
        //}

        //private async Task MapCode(CodeLinkModel model, CodeLink dbEntry)
        //{
        //    dbEntry.CodeDisplay = model.CodeDisplay;
        //    dbEntry.CodeValue = model.CodeValue;
        //    dbEntry.CodeValueFormat = model.CodeValueFormat;

        //    foreach (var child in dbEntry.Children)
        //    {
        //        var found = model.Children?.FirstOrDefault(t => t.Id == child.ChildNode);
        //        if (found == null)
        //        {
        //            codeDbContext.Entry(child).State = EntityState.Deleted;
        //            continue;
        //        }

        //        child.Child.CodeDisplay = found!.CodeDisplay;
        //        child.Child.CodeValue = found.CodeValue;
        //    }

        //    foreach (var it in model.Children!.Where(t => !dbEntry.Children.Any(x => x.ChildNode == t.Id)))
        //    {
        //        var existing = model.Children.FirstOrDefault(t => t.CodeValue == it.CodeValue && t.Id > 0);
        //        if (existing != null)
        //        {
        //            dbEntry.Children.Add(new CodeLinkNode() { ChildNode = existing.Id, Parent = dbEntry });
        //        }
        //        else
        //        {
        //            var dbChildCode = codeDbContext.Codes.FirstOrDefault(t => t.CodeValue == it.CodeValue) ?? new CodeLink()
        //            {
        //                CodeDisplay = it.CodeDisplay,
        //                CodeValue = it.CodeValue,
        //            };

        //            dbChildCode.Ancestors = [new CodeLinkNode() { Parent = dbEntry, Child = dbChildCode }];

        //            if (dbChildCode.Id < 1)
        //            {
        //                codeDbContext.Codes.Add(dbChildCode);
        //                await codeDbContext.SaveChangesAsync();
        //            }
        //        }
        //    }
        //}
    }
}