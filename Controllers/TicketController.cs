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
    using WebApi.Services;

    [Authorize(Roles = "partener, admin")]
    public class TicketController : WebApiController2
    {
        private ComplaintSeriesDbContext complaintSeriesDbContext;
        private IStorageService storageService;
        public TicketController(
            ComplaintSeriesDbContext complaintSeriesDbContext,
            IStorageService storageService,
            ILogger<TicketController> logger) : base(logger)
        {
            this.complaintSeriesDbContext = complaintSeriesDbContext;
            this.storageService = storageService;
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
                        .Include(t => t.Tickets)
                        .ThenInclude(t => t.Images)
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


            var ticket = complaint.Tickets[0];

            if (ticket.ToDeleteImages != null)
            {
                foreach (var toDelete in ticket.ToDeleteImages)
                {
                    this.complaintSeriesDbContext.Entry(toDelete).State = EntityState.Deleted;
                    storageService.Delete(toDelete.Data);
                }
            }

            if (ticket.ToAddImages != null && ticket.ToAddImages.Count > 0)
            {
                foreach (var toAdd in ticket.ToAddImages)
                {
                    toAdd.Data = this.storageService.Save(toAdd.Data, toAdd.Title);
                    toAdd.Ticket = dbModel.Tickets[0];
                    this.complaintSeriesDbContext.Entry(toAdd).State = EntityState.Added;
                }
            }

            await this.complaintSeriesDbContext.SaveChangesAsync();
            return Ok(ComplaintSeriesModel.from(
                this.complaintSeriesDbContext.Complaints
                         .Include(t => t.Tickets)
                         .ThenInclude(t => t.codeLinks)
                         .Include(t => t.Tickets)
                         .ThenInclude(t => t.Images)
                         .AsSplitQuery()
                         .SingleOrDefault(t => t.Id == dbModel.Id)
                         ));
        }
    }
}