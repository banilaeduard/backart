using EntityDto;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WorkSheetServices;

namespace WebApi.Controllers
{
    [Authorize(Roles = "admin")]
    public class ReportsController : WebApiController2
    {
        const string contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

        public ReportsController(
            ILogger<TicketController> logger) : base(logger)
        {
        }

        [HttpPost("MergeDispozitii")]
        public IActionResult MergeDispozitii()
        {
            Dictionary<int, List<DispozitieLivrare>> disp = new();

            foreach (var file in Request.Form.Files)
            {
                using (var stream = file.OpenReadStream())
                {
                    var items = WorkbookReader.ReadWorkBook<DispozitieLivrare>(stream, 4);
                    disp.Add(items.First().NumarIntern, items);
                }
            }

            return File(WorkbookReportsService.GenerateReport(disp), contentType);
        }
    }
}