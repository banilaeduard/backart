namespace WebApi.Controllers
{
    using System.Threading.Tasks;
    using System.Linq;

    using Microsoft.EntityFrameworkCore;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.Extensions.Logging;

    using DataAccess.Context;
    using WebApi.Models;
    using Storage;
    using DataAccess.Entities;
    using System.IO;
    using SolrIndexing;
    using Piping;

    [Authorize(Roles = "partener, admin")]
    public class TicketController : WebApiController2
    {
        private ComplaintSeriesDbContext complaintSeriesDbContext;
        private IStorageService storageService;
        private SolrCRUD solrIndex;
        private EnrichService enrichService;
        public TicketController(
            ComplaintSeriesDbContext complaintSeriesDbContext,
            IStorageService storageService,
            SolrCRUD solrIndex,
            EnrichService enrichService,
            ILogger<TicketController> logger) : base(logger)
        {
            this.complaintSeriesDbContext = complaintSeriesDbContext;
            this.storageService = storageService;
            this.solrIndex = solrIndex;
            this.enrichService = enrichService;
        }

        [HttpGet("{page}/{pageSize}")]
        public IActionResult GetAll(int page, int pageSize, [FromQuery] int[] documentIds)
        {
            if(documentIds?.Length > 0)
            {
                return Ok(new
                {
                    count = complaintSeriesDbContext.Complaints.Count(),
                    complaints = complaintSeriesDbContext.Complaints
                                                        .Where(complaint => documentIds.Contains(complaint.Id))
                                                        .Select(t => ComplaintSeriesModel.from(t, null))
                });
            }

            var complaints = complaintSeriesDbContext.Complaints
                        .OrderByDescending(t => t.CreatedDate)
                        .Skip((page - 1) * pageSize)
                        .Take(pageSize);

            return Ok(new
            {
                count = complaintSeriesDbContext.Complaints.Count(),
                complaints = complaints.Select(t => ComplaintSeriesModel.from(t, null))
            });
        }

        [HttpPost("delete")]
        public async Task<IActionResult> Delete(ComplaintSeriesModel complaint)
        {
            var dbModel = complaint.toDbModel();
            complaintSeriesDbContext.Remove(dbModel);
            await solrIndex.deleteDocument(dbModel.Id.ToString());
            await complaintSeriesDbContext.SaveChangesAsync();
            return Ok();
        }

        [HttpPost("status/{status}")]
        public async Task<IActionResult> UpdateStatus(ComplaintSeriesModel complaint, string status)
        {
            var dbModel = complaintSeriesDbContext.Find<ComplaintSeries>(complaint.Id);
            dbModel.Status = status;
            await complaintSeriesDbContext.SaveChangesAsync();
            return Ok(ComplaintSeriesModel.from(complaintSeriesDbContext.Find<ComplaintSeries>(complaint.Id), null));
        }

        [HttpPost]
        public async Task<IActionResult> SaveComplaint(ComplaintSeriesModel complaint)
        {
            var dbModel = complaint.toDbModel();

            if (complaint.Id < 1)
                complaintSeriesDbContext.Complaints.Add(dbModel);
            else
                complaintSeriesDbContext.Complaints.Update(dbModel);


            var ticket = complaint.Tickets[0];

            if (ticket.ToDeleteAttachment != null)
            {
                foreach (var toDelete in ticket.ToDeleteAttachment)
                {
                    complaintSeriesDbContext.Entry(toDelete).State = EntityState.Deleted;
                    storageService.Delete(toDelete.Data);
                }
            }

            if (ticket.ToAddAttachment != null && ticket.ToAddAttachment.Count > 0)
            {
                foreach (var toAdd in ticket.ToAddAttachment)
                {
                    toAdd.Data = storageService.SaveBase64(toAdd.Data, toAdd.Title);
                    toAdd.ContentType = toAdd.ContentType;
                    toAdd.Extension = Path.GetExtension(toAdd.Title);
                    toAdd.Ticket = dbModel.Tickets[0];
                    toAdd.StorageType = storageService.StorageType;
                    complaintSeriesDbContext.Entry(toAdd).State = EntityState.Added;
                }
            }

            await complaintSeriesDbContext.SaveChangesAsync();
            await enrichService.Enrich(dbModel.Tickets[0], dbModel);

            return Ok(ComplaintSeriesModel.from(
                complaintSeriesDbContext.Find<ComplaintSeries>(dbModel.Id), null)
                );
        }
    }
}
