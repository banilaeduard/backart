namespace CronJob
{
    using MimeKit;
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    using core;
    using CronJob.Services.FeedServices;
    using Microsoft.Extensions.DependencyInjection;

    public class EmailReaderCronJob : CronJobService<MimeMessage>
    {
        YahooEmailService mailSvc;
        public EmailReaderCronJob(IServiceProvider services, AppSettings settings) : base(settings.mailreccurencepattern, services)
        {
            mailSvc = new YahooEmailService(settings);
        }

        public async override Task DoWork(CancellationToken cancellationToken, IProcessor<MimeMessage> processor, IServiceScope scope)
        {
            await mailSvc.ReadDedMails(processor, cancellationToken);
        }
    }
}