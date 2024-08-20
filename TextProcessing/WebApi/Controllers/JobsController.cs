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
        public JobsController(
            ILogger<JobsController> logger,
            JobStatusContext jobStatusContext) : base(logger)
        {
            this.jobStatusContext = jobStatusContext;
        }

        [HttpGet()]
        public IActionResult Statuses()
        {
            try
            {
                return Ok(jobStatusContext.JobStatus.Where(t => t.CreatedDate > DateTime.Now.AddDays(-2)).Take(1000).ToList()
                    .GroupBy(t => t.CorelationId).OrderByDescending(t => t.First().CreatedDate).SelectMany(t => t.ToList())
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
                await ActorProxy.Create<IYahooFeederJob>(new ActorId("yM"), "").ReadMails(new MailSettings()
                {
                    Folders = Environment.GetEnvironmentVariable("y_folders")!.Split(";", StringSplitOptions.TrimEntries),
                    From = Environment.GetEnvironmentVariable("y_from")!.Split(";", StringSplitOptions.TrimEntries)
                }, CancellationToken.None);
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
