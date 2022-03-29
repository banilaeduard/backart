using System;
using System.Threading;
using System.Threading.Tasks;
using MailKit;
using MailKit.Net.Imap;
using MailKit.Search;
using MimeKit;
using core;
using System.Collections.Generic;

namespace CronJob.Services.FeedServices
{
    internal class YahooEmailService
    {
        private AppSettings appSettings;

        Dictionary<string, DateTime> processedMessages = new Dictionary<string, DateTime>();
        public YahooEmailService(AppSettings appSettings)
        {
            this.appSettings = appSettings;
        }
        public async Task ReadDedMails(IProcessor<MimeMessage> processor, CancellationToken cancellationToken)
        {
            DateTime defaultDate = DateTime.Now.AddDays(-int.Parse(appSettings.daysoffset));
            try
            {
                using (ImapClient client = new ImapClient())
                {
                    client.Connect("imap.mail.yahoo.com", 993, true); //For SSL
                    client.Authenticate(appSettings.yappuser, appSettings.yapppass);

                    foreach (var folder in GetFolders(client, cancellationToken))
                    {
                        foreach (var from in appSettings.fromContains.Split(';'))
                        {
                            bool success = false;
                            DateTime fromDate = DateTime.Now.AddDays(-int.Parse(appSettings.daysoffset));

                            if (processedMessages.ContainsKey(getKey(folder, from)))
                            {
                                fromDate = processedMessages[getKey(folder, from)];
                            }

                            folder.Open(FolderAccess.ReadOnly);
                            Console.WriteLine("{0} - {1}", folder.Name, from);

                            var uids = folder.Search(
                                SearchQuery.DeliveredAfter(fromDate).And(
                                  SearchQuery.FromContains(from)
                                )
                            , cancellationToken);
                            try
                            {
                                foreach (var uid in uids)
                                {
                                    cancellationToken.ThrowIfCancellationRequested();

                                    if (await processor.shouldProcess(null, uid.Id.ToString()))
                                    {
                                        var message = folder.GetMessage(uid);
                                        await processor.process(message, uid.Id.ToString());
                                        Console.WriteLine("From: {0}", message.From.ToString());
                                        Console.WriteLine("Subject: {0}\r\n", message.Subject);
                                    }
                                    else
                                    {
                                        Console.WriteLine("Skipping: {0}\r\n", uid.Id);
                                    }
                                }

                                success = true;
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine(ex.Message);
                            }
                            finally
                            {
                                if (success)
                                {
                                    processedMessages[getKey(folder, from)] = DateTime.Now.AddHours(-1);
                                }
                                folder.Close();
                            }
                        }
                    }
                }
            }
            catch (Exception ep)
            {
                Console.WriteLine(ep.Message);
            }
        }

        string getKey(IMailFolder folder, string from)
        {
            return string.Format("{0}_{1}", folder.Name, from);
        }

        IEnumerable<IMailFolder> GetFolders(ImapClient client, CancellationToken cancellationToken)
        {
            var personal = client.GetFolder(client.PersonalNamespaces[0]);

            yield return client.Inbox;

            foreach (var folder in appSettings.mailfolders.Split(','))
            {
                yield return personal.GetSubfolder(folder, cancellationToken);
            }
        }
    }
}