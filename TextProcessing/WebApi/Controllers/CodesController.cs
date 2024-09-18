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
    using ServiceImplementation;

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
            dynamic map(CodeLink t)
            {
                return new
                {
                    id = t.Id,
                    codeDisplay = t.CodeDisplay,
                    codeValue = t.CodeValue,
                    codeValueFormat = t.CodeValueFormat,
                    t.isRoot,
                    children = t.Children?.Select(t => map(t.Child)).OrderBy(t => t.codeValueFormat),
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
        public async Task<IActionResult> SaveCodes(List<CodeLink> codes)
        {
            this.codeDbContext.Codes.AddRange(codes);
            await this.codeDbContext.SaveChangesAsync();

            return Ok(codes);
        }

        [HttpPatch]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> UpdateCodes(List<CodeLink> codes)
        {
            this.codeDbContext.Codes.UpdateRange(codes);
            await this.codeDbContext.SaveChangesAsync();

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
                              .State = Microsoft.EntityFrameworkCore.EntityState.Deleted);
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

                foreach(var missing in items.Where(t => existingRootCodes[t.NumeArticol].Children.FirstOrDefault(x => x.Child.CodeValue == t.CodColet) == null))
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
    }
}