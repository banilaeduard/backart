namespace WebApi.Controllers
{
    using System.Threading.Tasks;
    using System.Linq;

    using Microsoft.EntityFrameworkCore;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.Extensions.Logging;

    using WebApi.Entities;
    using WebApi.Models;

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
                        .ThenInclude(t => t.codeLinks)
                        .Skip((page - 1) * pageSize)
                        .Take(pageSize)
                        .Select(t => ComplaintSeriesModel.from(t, false))
            });
        }

        [HttpPost]
        public async Task<IActionResult> SaveComplaint(ComplaintSeriesModel complaint)
        {
            var dbComp = this.complaintSeriesDbContext.Complaints.Where(t => t.Id == complaint.Id).SingleOrDefault()
                ?? new ComplaintSeries();

            if (complaint.ToAddTickets != null && complaint.ToAddTickets.Count() > 0)
            {
                dbComp.Tickets.AddRange(complaint.ToAddTickets.Select(t => t.toDbModel()));
            }

            if (complaint.ToRemoveTickets != null && complaint.ToRemoveTickets.Count() > 0)
            {
                complaint.ToRemoveTickets.ForEach(t =>
                                complaintSeriesDbContext.Entry(t.toDbModel()).State = EntityState.Deleted);
            }

            if (complaint.Tickets != null && complaint.Tickets.Count() > 0)
            {
                complaint.Tickets.ForEach(t => complaintSeriesDbContext.Entry(t.toDbModel()));
            }

            if (complaint.Id > 0)
                this.complaintSeriesDbContext.Update(complaint);
            else
                this.complaintSeriesDbContext.Add(complaint);

            await this.complaintSeriesDbContext.SaveChangesAsync();
            return Ok(ComplaintSeriesModel.from(this.complaintSeriesDbContext.Complaints
                         .Include(t => t.Tickets)
                         .ThenInclude(t => t.codeLinks)
                         .Include(t => t.Tickets)
                         .ThenInclude(t => t.Images)
                         .AsSplitQuery()
                         .FirstOrDefault(t => t.Id == complaint.Id), true)
                );
        }

        [HttpPost("images")]
        public IActionResult fetchImages(ComplaintSeriesModel complaint)
        {
            var t = this.complaintSeriesDbContext.Complaints
                        .Include(t => t.Tickets)
                        .ThenInclude(t => t.codeLinks)
                        .Include(t => t.Tickets)
                        .ThenInclude(t => t.Images)
                        .AsSplitQuery()
                        .FirstOrDefault(t => t.Id == complaint.Id);
            return Ok(ComplaintSeriesModel.from(t, true));
        }
    }
}