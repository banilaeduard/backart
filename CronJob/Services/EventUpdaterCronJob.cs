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
        static string exp = "0 0/55 * 1/1 * ? *";
        public EventUpdaterCronJob(IServiceProvider services) : base(exp, services)
        {
        }
        public override async Task DoWork(CancellationToken cancellationToken, IProcessor<ComplaintSeries> processor, IServiceScope scope)
        {
            try
            {
                await new ComplaintSeriesEventService(
                    scope.ServiceProvider.GetRequiredService<DbContextOptions<ComplaintSeriesDbContext>>(),
                    scope.ServiceProvider.GetRequiredService<NoFilterBaseContext>()
                    ).FeedComplaints(processor, cancellationToken);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
    }
}