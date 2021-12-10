namespace WebApi.Controllers
{
    using System.Threading.Tasks;
    using System.Collections.Generic;

    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.Extensions.Logging;

    using WebApi.Entities;

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
            return Ok(this.codeDbContext.Codes);
        }

        [HttpPost]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> SaveCodes(List<Code> codes)
        {
            this.codeDbContext.Codes.AddRange(codes);
            await this.codeDbContext.SaveChangesAsync();

            return Ok(codes);
        }

        [HttpPatch]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> UpdateCodes(List<Code> codes)
        {
            this.codeDbContext.Codes.UpdateRange(codes);
            await this.codeDbContext.SaveChangesAsync();

            return Ok(codes);
        }

        [HttpPost("delete")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> DeleteCodes(List<Code> codes)
        {
            this.codeDbContext.Codes.RemoveRange(codes);
            await this.codeDbContext.SaveChangesAsync();

            return Ok();
        }
    }
}