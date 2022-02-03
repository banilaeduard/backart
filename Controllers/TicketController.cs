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
                        .Select(t => ComplaintSeriesModel.from(t))
            });
        }

        [HttpPost]
        public async Task<IActionResult> SaveComplaint(ComplaintSeriesModel complaint)
        {
            var dbModel = complaint.toDbModel();
            if (complaint.Id < 1)
                this.complaintSeriesDbContext.Complaints.Add(dbModel);
            else
                this.complaintSeriesDbContext.Complaints.Update(dbModel);

            await this.complaintSeriesDbContext.SaveChangesAsync();
            return Ok(ComplaintSeriesModel.from(
                this.complaintSeriesDbContext.Complaints
                         .Include(t => t.Tickets)
                         .ThenInclude(t => t.codeLinks)
                         .AsSplitQuery()
                         .SingleOrDefault(t => t.Id == dbModel.Id)
                         ));
        }

        [HttpPost("images")]
        public IActionResult fetchDetails(TicketModel ticketModel)
        {
            var ticketDb = this.complaintSeriesDbContext.Ticket
                        .Include(t => t.codeLinks)
                        .Include(t => t.Images)
                        .AsSplitQuery()
                        .FirstOrDefault(t => t.Id == ticketModel.Id);
            return Ok(TicketModel.from(ticketDb));
        }
    }
}