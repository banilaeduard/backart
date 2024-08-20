namespace WebApi.Controllers
{
    using System.Threading.Tasks;
    using System.Linq;

    using Microsoft.EntityFrameworkCore;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.Extensions.Logging;

    using DataAccess.Context;
    using DataAccess.Entities;
    using System.IO;
    using global::WebApi.Models;

    [Authorize(Roles = "partener, admin")]
    public class TicketController : WebApiController2
    {
        private ComplaintSeriesDbContext complaintSeriesDbContext;
        public TicketController(
            ComplaintSeriesDbContext complaintSeriesDbContext,

            ILogger<TicketController> logger) : base(logger)
        {
            this.complaintSeriesDbContext = complaintSeriesDbContext;
        }

        [HttpGet("{page}/{pageSize}")]
        public IActionResult GetAll(int page, int pageSize)
        {
            var complaints = complaintSeriesDbContext.Complaints
                        .OrderByDescending(t => t.CreatedDate)
                        .Skip((page - 1) * pageSize)
                        .Take(pageSize)
                        .Include(t => t.Tickets)
                        .ThenInclude(t => t.Attachments)
                        .Include(t => t.Tickets)
                        .ThenInclude(t => t.CodeLinks)
                        .Select(t => ComplaintSeriesModel.from(t));

            return Ok(new
            {
                count = complaintSeriesDbContext.Complaints.Count(),
                complaints
            });
        }

        [HttpDelete("delete/{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var item = complaintSeriesDbContext.Complaints.Where(t => t.Id == id).First();
            complaintSeriesDbContext.Remove(item);
            await complaintSeriesDbContext.SaveChangesAsync();
            return Ok();
        }

        [HttpPost("status/{status}")]
        public async Task<IActionResult> UpdateStatus(ComplaintSeriesModel complaint, string status)
        {
            var dbModel = complaintSeriesDbContext.Find<ComplaintSeries>(complaint.Id)!;
            dbModel.Status = status;
            await complaintSeriesDbContext.SaveChangesAsync();
            return Ok(ComplaintSeriesModel.from(complaintSeriesDbContext.Find<ComplaintSeries>(complaint.Id)!));
        }

        [HttpPost]
        public async Task<IActionResult> SaveComplaint(ComplaintSeriesModel complaint)
        {
            ComplaintSeries dbModel;

            if (complaint.Id < 1)
            {
                dbModel = complaint.toDbModel();
                complaintSeriesDbContext.Complaints.Add(dbModel);
            }
            else
            {
                dbModel = complaintSeriesDbContext.Find<ComplaintSeries>(complaint.Id)!;
                complaint.toDbModel(dbModel);
            }

            var ticket = complaint.Tickets[0];

            if (ticket.ToDeleteAttachment != null)
            {
                foreach (var toDelete in ticket.ToDeleteAttachment)
                {
                    complaintSeriesDbContext.Entry(toDelete).State = EntityState.Deleted;
                    //storageService.Delete(toDelete.Data);
                }
            }

            if (ticket.ToAddAttachment != null && ticket.ToAddAttachment.Count > 0)
            {
                foreach (var toAdd in ticket.ToAddAttachment)
                {
                    //toAdd.Data = storageService.SaveBase64(toAdd.Data, toAdd.Title);
                    toAdd.ContentType = toAdd.ContentType;
                    toAdd.Extension = Path.GetExtension(toAdd.Title);
                    toAdd.Ticket = dbModel.Tickets[0];
                    //toAdd.StorageType = storageService.StorageType;
                    complaintSeriesDbContext.Entry(toAdd).State = EntityState.Added;
                }
            }

            await complaintSeriesDbContext.SaveChangesAsync();

            return Ok();
        }
    }
}
