using DataAccess.Entities;
using Microsoft.Extensions.DependencyInjection;
using MimeKit;

namespace CronJob
{
    public static class CollectionExtension
    {
        public static IServiceCollection configureCronJob(this IServiceCollection services)
        {
            services.AddScoped<IProcessor<MimeMessage>, EmailProcessor>();
            services.AddScoped<IProcessor<ComplaintSeries>, GCalendarServiceProcessor>();
            services.AddHostedService<EmailReaderCronJob>();
            // services.AddHostedService<EventUpdaterCronJob>();
            return services;
        }
    }
}
