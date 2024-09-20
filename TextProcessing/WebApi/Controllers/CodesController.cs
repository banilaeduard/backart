namespace WebApi.Controllers
{
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Authorization;

    using System.Threading.Tasks;
    using System.Collections.Generic;
    using Microsoft.Extensions.Logging;


    using DataAccess.Entities;
    using DataAccess.Context;
    using System.Linq;
    using EntityDto;
    using Microsoft.EntityFrameworkCore;
    using WorkSheetServices;
    using global::WebApi.Models;

    public class CodesController : WebApiController2
    {
        private CodeDbContext codeDbContext;
        public CodesController(CodeDbContext codeDbContext,
        ILogger<CodesController> logger) : base(logger)
        {
            this.codeDbContext = codeDbContext;
        }

        [HttpGet]
        [Authorize(Roles = "partener, admin")]
        public IActionResult GetCodes()
        {
            CodeLinkModel map(CodeLink t)
            {
                return new CodeLinkModel()
                {
                    Id = t.Id,
                    CodeDisplay = t.CodeDisplay,
                    CodeValue = t.CodeValue,
                    isRoot = t.isRoot,
                    Children = t.Children?.Select(t => map(t.Child)).ToList(),
                    //barcode = BarCodeGenerator.GenerateDataUrlBarCode(t.CodeValue)
                };
            }

            var result = this.codeDbContext.Codes
                .Include(t => t.Children)
                .ThenInclude(t => t.Child)
                .ToList()
                .Select(map);

            return Ok(result);
        }

        [HttpGet("attributes")]
        [Authorize(Roles = "partener, admin")]
        public IActionResult GetCodesAttributes()
        {
            return Ok(this.codeDbContext.CodeAttribute);
        }

        [HttpPost]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> SaveCodes(List<CodeLinkModel> codes)
        {
            var codeDb = codeDbContext.Codes.FirstOrDefault(t => codes[0].CodeValue == t.CodeValue);
            if (codeDb != null) return BadRequest("Code already exists");

            var code = codes[0];

            codeDb = new CodeLink()
            {
                isRoot = true,
                Children = []
            };
            codeDb = codeDbContext.Add(codeDb).Entity;

            await MapCode(code, codeDb);

            await codeDbContext.SaveChangesAsync();
            return Ok(codes);
        }

        [HttpPatch]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> UpdateCodes(List<CodeLinkModel> codes)
        {
            var codeDb = codeDbContext.Codes.Where(t => codes[0].Id == t.Id).Include(t => t.Children).ThenInclude(t => t.Child).ToList();

            foreach (var item in codes)
            {
                var dbEntry = codeDb.Where(t => t.Id == item.Id).First();
                await MapCode(item, dbEntry);
            }

            await codeDbContext.SaveChangesAsync();
            return Ok(codes);
        }

        [HttpPost("delete")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> DeleteCodes(List<CodeLink> codes)
        {
            this.codeDbContext.Codes.RemoveRange(codes);
            await this.codeDbContext.SaveChangesAsync();

            return Ok();
        }

        [HttpPost("attributes")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> SaveCodesAttributes(List<CodeAttribute> codes)
        {
            this.codeDbContext.AddRange(codes);
            await this.codeDbContext.SaveChangesAsync();

            return Ok(codes);
        }

        [HttpPatch("attributes")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> UpdateCodesAttributes(List<CodeAttribute> codes)
        {
            this.codeDbContext.UpdateRange(codes);
            await this.codeDbContext.SaveChangesAsync();

            return Ok(codes);
        }

        [HttpPost("attributes/delete")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> DeleteCodesAttributes(List<CodeAttribute> codes)
        {
            codes.ForEach(code =>
            this.codeDbContext.Entry(new CodeAttribute() { Tag = code.Tag, InnerValue = code.InnerValue })
                              .State = EntityState.Deleted);
            await this.codeDbContext.SaveChangesAsync();

            return Ok();
        }

        [HttpPost("upload"), DisableRequestSizeLimit]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> UploadStructure()
        {

            var file = Request.Form.Files[0];

            using (var stream = file.OpenReadStream())
            {
                var items = WorkbookReader.ReadWorkBook<StructuraCod>(stream, 2);
                var existingRootCodes = codeDbContext.Codes.Where(t => t.isRoot).AsNoTracking().ToDictionary(t => t.CodeDisplay, t => t);

                foreach (var code in items.Select(t => t.NumeArticol).Distinct())
                {
                    if (!existingRootCodes.ContainsKey(code))
                    {
                        codeDbContext.Codes.Add(new CodeLink() { CodeValue = code, CodeDisplay = code, isRoot = true });
                    }
                }
                await codeDbContext.SaveChangesAsync();

                var childNodes = codeDbContext.Codes.Where(t => !t.isRoot).AsNoTracking().ToDictionary(t => t.CodeValue, t => t);

                foreach (var item in items.DistinctBy(t => t.CodColet))
                {
                    if (!childNodes.ContainsKey(item.CodColet))
                    {
                        codeDbContext.Codes.Add(new CodeLink { CodeValue = item.CodColet, CodeDisplay = item.NumeColet, isRoot = false, CodeValueFormat = item.NumarColet });
                    }
                }

                await codeDbContext.SaveChangesAsync();

                existingRootCodes = codeDbContext.Codes.Where(t => t.isRoot).Include(t => t.Children).ThenInclude(t => t.Child).ToDictionary(t => t.CodeDisplay, t => t);

                foreach (var missing in items.Where(t => existingRootCodes[t.NumeArticol].Children.FirstOrDefault(x => x.Child.CodeValue == t.CodColet) == null))
                {
                    existingRootCodes[missing.NumeArticol].Children.Add(new CodeLinkNode()
                    {
                        Child = codeDbContext.Codes.First(t => t.CodeValue == missing.CodColet),
                        Parent = existingRootCodes[missing.NumeArticol]
                    });
                }

                await codeDbContext.SaveChangesAsync();
            }

            return Ok();
        }

        private async Task MapCode(CodeLinkModel model, CodeLink dbEntry)
        {
            dbEntry.CodeDisplay = model.CodeDisplay;
            dbEntry.CodeValue = model.CodeValue;

            foreach (var child in dbEntry.Children)
            {
                var found = model.Children?.FirstOrDefault(t => t.Id == child.ChildNode);
                if (found == null)
                {
                    codeDbContext.Entry(child).State = EntityState.Deleted;
                    continue;
                }

                child.Child.CodeDisplay = found!.CodeDisplay;
                child.Child.CodeValue = found.CodeValue;
            }

            foreach (var it in model.Children!.Where(t => !dbEntry.Children.Any(x => x.ChildNode == t.Id)))
            {
                var existing = model.Children.FirstOrDefault(t => t.CodeValue == it.CodeValue && t.Id > 0);
                if (existing != null)
                {
                    dbEntry.Children.Add(new CodeLinkNode() { ChildNode = existing.Id, Parent = dbEntry });
                }
                else
                {
                    var dbChildCode = codeDbContext.Codes.FirstOrDefault(t => t.CodeValue == it.CodeValue) ?? new CodeLink()
                    {
                        CodeDisplay = it.CodeDisplay,
                        CodeValue = it.CodeValue,
                    };

                    dbChildCode.Ancestors = [new CodeLinkNode() { Parent = dbEntry, Child = dbChildCode }];

                    if (dbChildCode.Id < 1)
                    {
                        codeDbContext.Codes.Add(dbChildCode);
                        await codeDbContext.SaveChangesAsync();
                    }
                }
            }
        }
    }
}