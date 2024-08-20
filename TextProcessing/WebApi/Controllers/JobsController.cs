using DataAccess.Context;
using Microsoft.AspNetCore.Mvc;
using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Actors.Client;
using YahooFeederJob.Interfaces;

namespace WebApi.Controllers
{
    public class JobsController : WebApiController2
    {
        private JobStatusContext jobStatusContext;
        private MailSettings mailSettings;
        public JobsController(
            ILogger<JobsController> logger,
            JobStatusContext jobStatusContext,
            MailSettings settings) : base(logger)
        {
            this.jobStatusContext = jobStatusContext;
            this.mailSettings = settings;
        }

        [HttpGet()]
        public IActionResult Statuses()
        {
            try
            {
                return Ok(
                    jobStatusContext.JobStatus
                    .Where(t => t.CreatedDate > DateTime.Now.AddDays(-2))
                    .Take(1000)
                    .ToList()
                    .GroupBy(t => t.CorelationId).OrderByDescending(t => t.First().CreatedDate)
                    );
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return NotFound();
            }
        }

        [HttpGet("trigger")]
        public async Task<IActionResult> Trigger()
        {
            try
            {
                await ActorProxy.Create<IYahooFeederJob>(new ActorId("yM"), "").ReadMails(mailSettings, CancellationToken.None);
                return Ok("Finished");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return NotFound();
            }
        }
    }
}
