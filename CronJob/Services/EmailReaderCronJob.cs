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
        public EmailReaderCronJob(IServiceProvider services, AppSettings settings) : base(settings.mailreccurencepattern, services, settings)
        {
            mailSvc = new YahooEmailService(settings);
        }

        public async override Task<Boolean> DoWork(CancellationToken cancellationToken, IProcessor<MimeMessage> processor, IServiceScope scope)
        {
            try
            {
                await mailSvc.ReadDedMails(processor, cancellationToken);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return false;
            }
        }
    }
}
