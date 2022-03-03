using System;
using System.Threading;
using System.Threading.Tasks;
using MailKit;
using MailKit.Net.Imap;
using MailKit.Search;
using MimeKit;
using core;

namespace CronJob.Services.FeedServices
{
    internal class YahooEmailService
    {
        private AppSettings appSettings;
        public YahooEmailService(AppSettings appSettings)
        {
            this.appSettings = appSettings;
        }
        public async Task ReadDedMails(IProcessor<MimeMessage> processor, CancellationToken cancellationToken)
        {
            try
            {
                using (ImapClient client = new ImapClient())
                {
                    client.Connect("imap.mail.yahoo.com", 993, true); //For SSL                
                    client.Authenticate(appSettings.yappuser, appSettings.yapppass);

                    client.Inbox.Open(FolderAccess.ReadOnly);
                    var uids = client.Inbox.Search(
                        SearchQuery.FromContains("dedeman.ro").And(
                            SearchQuery.DeliveredAfter(DateTime.Now.AddDays(-7))
                            )
                        );
                    foreach (var uid in uids)
                    {
                        if (cancellationToken.IsCancellationRequested)
                        {
                            return;
                        }

                        if (await processor.shouldProcess(null, uid.Id.ToString()))
                        {
                            var message = client.Inbox.GetMessage(uid);
                            await processor.process(message, uid.Id.ToString());
                            Console.WriteLine("From: {0}", message.From.ToString());
                            Console.WriteLine("Subject: {0}\r\n", message.Subject);
                        }
                        else
                        {
                            Console.WriteLine("Skipping: {0}\r\n", uid.Id);
                        }
                    }
                }
            }
            catch (Exception ep)
            {
                Console.WriteLine(ep.Message);
            }
        }
    }
}