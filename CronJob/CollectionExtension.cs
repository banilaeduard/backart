using Microsoft.Extensions.DependencyInjection;
using MimeKit;

namespace CronJob
{
    public static class CollectionExtension
    {
            public static IServiceCollection configureCronJob(this IServiceCollection services)
            {
                services.AddScoped<IProcessor<MimeMessage>, EmailProcessor>();
                return services;
            }
    }
}
