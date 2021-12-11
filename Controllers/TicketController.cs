namespace WebApi.Controllers
{
    using System.Threading.Tasks;
    using System.Linq;

    using Microsoft.EntityFrameworkCore;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.Extensions.Logging;

    using WebApi.Entities;

    [Authorize(Roles = "partener, admin")]
    public class TicketController : WebApiController2
    {
        private ComplaintSeriesDbContext complaintSeriesDbContext;
        public TicketController(ComplaintSeriesDbContext complaintSeriesDbContext,
        ILogger<TicketController> logger) : base(logger)
        {
            this.complaintSeriesDbContext = complaintSeriesDbContext;
        }

        [HttpGet]
        public IActionResult GetAll()
        {
            return Ok(this.complaintSeriesDbContext.Complaints
                        .Include(t => t.Tickets)
                        .ThenInclude(t => t.Code)
                        .OrderByDescending(t => t.Id));
        }

        [HttpPost]
        public async Task<IActionResult> SaveComplaint(ComplaintSeries complaint)
        {
            if (complaint.Id > 0)
                this.complaintSeriesDbContext.Update(complaint);
            else
                this.complaintSeriesDbContext.Add(complaint);

            await this.complaintSeriesDbContext.SaveChangesAsync();
            return Ok(complaint);
        }

        [HttpPost("images")]
        public IActionResult fetchImages(ComplaintSeries complaint)
        {
            return Ok(this.complaintSeriesDbContext.Complaints
                        .Include(t => t.Tickets)
                        .ThenInclude(t => t.Code)
                        .Include(t => t.Tickets)
                        .ThenInclude(t => t.Images)
                        .AsSplitQuery()
                        .FirstOrDefault(t => t.Id == complaint.Id)
                        );
        }
    }
}