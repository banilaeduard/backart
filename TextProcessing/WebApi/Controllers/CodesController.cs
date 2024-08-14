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
            var result = this.codeDbContext.Codes.ToList();
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
    }
}