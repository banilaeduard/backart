namespace WebApi.Controllers
{
    using System.Threading.Tasks;
    using System.Linq;

    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.Extensions.Logging;

    using RepositoryContract.Tickets;
    using global::WebApi.Models;
    using global::Services.Storage;
    using System.Text;

    [Authorize(Roles = "partener, admin")]
    public class TicketController : WebApiController2
    {
        private ITicketEntryRepository ticketEntryRepository;
        private IStorageService storageService;
        public TicketController(
            ITicketEntryRepository ticketEntryRepository,
            IStorageService storageService,
            ILogger<TicketController> logger) : base(logger)
        {
            this.ticketEntryRepository = ticketEntryRepository;
            this.storageService = storageService;
        }

        [HttpGet("{page}/{pageSize}")]
        public async Task<IActionResult> GetAll(int page, int pageSize)
        {
            var complaints = await ticketEntryRepository.GetAll(page, pageSize);

            return Ok(new
            {
                count = complaints.Count,
                complaints = complaints.OrderByDescending(t => t.CreatedDate).Skip((page - 1) * pageSize).Take(pageSize)
                .Select(t => TicketSeriesModel.from(t, Encoding.UTF8.GetString(storageService.Access(t.Description, out var contentType))))
            });
        }

        [HttpDelete("delete/{partitionKey}/{rowKey}")]
        public async Task<IActionResult> Delete(string partitionKey, string rowKey)
        {
            await ticketEntryRepository.Delete<TicketEntity>(partitionKey, rowKey);
            return Ok();
        }

        [HttpPost]
        public async Task<IActionResult> SaveComplaint(TicketSeriesModel complaint)
        {
            //await ticketEntryRepository.Save(complaint);
            return Ok();
        }
    }
}
