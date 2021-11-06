using core;
using CronJob.Services.FeedServices;
using DataAccess;
using DataAccess.Context;
using DataAccess.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace CronJob
{
    public class EventUpdaterCronJob : CronJobService<ComplaintSeries>
    {
        public EventUpdaterCronJob(IServiceProvider services, AppSettings appsetting) : base(appsetting.calreccurencepattern, services, appsetting)
        {
        }
        public override async Task<Boolean> DoWork(CancellationToken cancellationToken, IProcessor<ComplaintSeries> processor, IServiceScope scope)
        {
            try
            {
                await new ComplaintSeriesEventService(
                    scope.ServiceProvider.GetRequiredService<DbContextOptions<ComplaintSeriesDbContext>>(),
                    scope.ServiceProvider.GetRequiredService<NoFilterBaseContext>()
                    ).FeedComplaints(processor, cancellationToken);

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return false;
            }
        }
    }
}