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

        [HttpGet("{page}/{pageSize}")]
        public IActionResult GetAll(int page, int pageSize)
        {
            return Ok(new
            {
                count = this.complaintSeriesDbContext.Complaints.Count(),
                complaints = this.complaintSeriesDbContext.Complaints
                        .OrderByDescending(t => t.Id)
                        .Include(t => t.Tickets)
                        .ThenInclude(t => t.Code)
                        .Skip((page - 1) * pageSize)
                        .Take(pageSize)
            });
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