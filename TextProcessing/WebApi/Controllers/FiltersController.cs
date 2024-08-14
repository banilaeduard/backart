using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApi.Models;
using DataAccess.Context;

namespace WebApi.Controllers
{
    [Authorize(Roles = "partener, admin")]
    public class FiltersController : WebApiController2
    {
        FilterDbContext filterContext;
        public FiltersController(
            FilterDbContext filterContext,
            ILogger<FiltersController> logger) : base(logger)
        {
            this.filterContext = filterContext;
        }

        [HttpGet("{id}")]
        public IActionResult getById(int id)
        {
            try
            {
                return Ok(FilterModel.From(filterContext.Filters.FirstOrDefault(t => t.Id == id)));
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return NotFound();
            }
        }

        [HttpGet]
        public IActionResult get()
        {
            return Ok(filterContext.Filters.OrderByDescending(t => t.CreatedDate).Select(t => FilterModel.From(t)));
        }

        [HttpPost("delete")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> delete(FilterModel filterModel)
        {
            filterContext.Filters.Remove(filterModel.toDatabaseModel());
            await filterContext.SaveChangesAsync();
            return Ok();
        }

        [HttpPost("update")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> update(FilterModel filterModel)
        {
            var dbModel = filterModel.toDatabaseModel();
            filterContext.Filters.Update(dbModel);
            await filterContext.SaveChangesAsync();
            return Ok(dbModel);
        }

        [HttpPost]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> create(FilterModel filterModel)
        {
            var filterDb = filterModel.toDatabaseModel();

            filterContext.Filters.Add(filterDb);
            await filterContext.SaveChangesAsync();

            return Ok(FilterModel.From(filterDb));
        }
    }
}
