using DataAccess.Context;
using DataAccess.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers
{
    [Authorize(Roles = "admin")]
    public class DataKeyLocationController : WebApiController2
    {
        AppIdentityDbContext ctx;
        public DataKeyLocationController(
            ILogger<DataKeyLocationController> logger,
            AppIdentityDbContext ctx
            ) : base(logger)
        {
            this.ctx = ctx;
        }

        [HttpGet]
        public async Task<IActionResult> GetLocations()
        {
            return Ok(ctx.DataKeyLocation.ToList());
        }

        [HttpPatch]
        public async Task<IActionResult> UpdateLocation([FromBody] DataKeyLocation location)
        {
            ctx.DataKeyLocation.Update(location);
            await ctx.SaveChangesAsync();
            return Ok(location);
        }

        [HttpPost]
        public async Task<IActionResult> AddLocation([FromBody] DataKeyLocation location)
        {
            await ctx.DataKeyLocation.AddAsync(location);
            await ctx.SaveChangesAsync();
            return Ok(location);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteLocation(string id)
        {
            var item = ctx.DataKeyLocation.Where(t => t.Id == id).First();
            ctx.DataKeyLocation.Remove(item);
            await ctx.SaveChangesAsync();
            return Ok();
        }
    }
}
