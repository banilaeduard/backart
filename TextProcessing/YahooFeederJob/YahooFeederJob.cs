using DataAccess.Context;
using DataAccess.Entities;
using Entities.Remoting.Jobs;
using MailKit;
using MailKit.Net.Imap;
using MailKit.Search;
using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Actors.Runtime;
using Microsoft.ServiceFabric.Services.Remoting.Client;
using Microsoft.ServiceFabric.Services.Remoting.V2.FabricTransport.Client;
using MimeKit;
using NER;
using System.Text.RegularExpressions;
using YahooFeederJob.Interfaces;

namespace YahooFeederJob
{
    /// <remarks>
    /// This class represents an actor.
    /// Every ActorID maps to an instance of this class.
    /// The StatePersistence attribute determines persistence and replication of actor state:
    ///  - Persisted: State is written to disk and replicated.
    ///  - Volatile: State is kept in memory only and replicated.
    ///  - None: State is kept in memory only and not replicated.
    /// </remarks>
    [StatePersistence(StatePersistence.Persisted)]
    internal class YahooFeederJob : Actor, IYahooFeederJob, IRemindable
    {
        private static Regex regexHtml = new Regex(@"(<br />|<br/>|</ br>|</br>)|<br>");
        private static Regex nrComanda = new Regex(@"^\d{10}$");
        private ServiceProxyFactory serviceProxy;
        private ComplaintSeriesDbContext complaintSeriesDbContext;
        private JobStatusContext jobStatusContext;

        /// <summary>
        /// Initializes a new instance of YahooFeederJob
        /// </summary>
        /// <param name="actorService">The Microsoft.ServiceFabric.Actors.Runtime.ActorService that will host this actor instance.</param>
        /// <param name="actorId">The Microsoft.ServiceFabric.Actors.ActorId for this actor instance.</param>
        public YahooFeederJob(ActorService actorService, ActorId actorId, ComplaintSeriesDbContext complaintSeriesDbContext, JobStatusContext jobStatusContext)
            : base(actorService, actorId)
        {
            this.complaintSeriesDbContext = complaintSeriesDbContext;
            this.jobStatusContext = jobStatusContext;
        }

        async Task IRemindable.ReceiveReminderAsync(string reminderName, byte[] state, TimeSpan dueTime, TimeSpan period)
        {
            try
            {
                var mailSettings = await StateManager.GetOrAddStateAsync("settings", new MailSettings() { Folders = ["Inbox"], From = ["dedeman.ro"] });
                ActorEventSource.Current.ActorMessage(this, "Reminder executed {0}--{1}.", reminderName, DateTime.UtcNow);
                await this.ReadMails(mailSettings, CancellationToken.None);
            }
            catch (Exception ex)
            {
                ActorEventSource.Current.ActorMessage(this, "Job error: {0}", ex);
            }
        }

        /// <summary>
        /// This method is called whenever an actor is activated.
        /// An actor is activated the first time any of its methods are invoked.
        /// </summary>
        protected override async Task OnActivateAsync()
        {
            await base.OnActivateAsync();

            ActorEventSource.Current.ActorMessage(this, "Actor activated.");

            serviceProxy = new ServiceProxyFactory((c) =>
            {
                return new FabricTransportServiceRemotingClientFactory();
            });
            var reminder = await this.RegisterReminderAsync("WORKWORK", BitConverter.GetBytes(100), TimeSpan.FromSeconds(60), TimeSpan.FromHours(3));
        }

        string getKey(IMailFolder folder, string from)
        {
            return string.Format("{0}_{1}", folder.Name.ToLowerInvariant(), from.ToLowerInvariant());
        }

        IEnumerable<IMailFolder> GetFolders(ImapClient client, string[] folders, CancellationToken cancellationToken)
        {
            var personal = client.GetFolder(client.PersonalNamespaces[0]);

            foreach (var folder in folders)
            {
                yield return personal.GetSubfolder(folder, cancellationToken);
            }
        }

        string getBody(MimeMessage message)
        {
            return !string.IsNullOrWhiteSpace(message.HtmlBody) ?
                                                    HtmlStripper.StripHtml(regexHtml.Replace(message.HtmlBody, " ")) : message.TextBody.Trim() ?? "";
        }

        async Task IYahooFeederJob.ReadMails(MailSettings settings, CancellationToken cancellationToken)
        {
            await this.ReadMails(settings, cancellationToken);
        }

        async Task ReadMails(MailSettings settings, CancellationToken cancellationToken)
        {
            var cfg = ActorService.Context.CodePackageActivationContext.GetConfigurationPackageObject("Config");
            var user = cfg.Settings.Sections["Yahoo"].Parameters["User"].Value;
            var pwd = cfg.Settings.Sections["Yahoo"].Parameters["Password"].Value;
            using (ImapClient client = new ImapClient())
            {
                await client.ConnectAsync(
                    "imap.mail.yahoo.com",
                    993,
                    true, cancellationToken); //For SSL
                await client.AuthenticateAsync(user, pwd, cancellationToken);
                foreach (var folder in GetFolders(client, settings.Folders, cancellationToken))
                {
                    foreach (var from in settings.From)
                    {
                        var key = getKey(folder, from);
                        try
                        {
                            IList<UniqueId> uids;

                            var latestProcessedDate = await this.StateManager.TryGetStateAsync<DateTime>(key, cancellationToken);
                            DateTime fromDate = latestProcessedDate.HasValue ? latestProcessedDate.Value : DateTime.Now.AddDays(-10);

                            this.jobStatusContext.JobStatus.Add(new JobStatusLog()
                            {
                                TenantId = "cubik",
                                Message = string.Format("Started Job from date {0} at {1}", fromDate, DateTime.Now),
                                CreatedDate = DateTime.Now,
                            });

                            folder.Open(FolderAccess.ReadOnly, cancellationToken);

                            uids = folder.Search(
                                SearchQuery.DeliveredAfter(fromDate).And(
                                  SearchQuery.FromContains(from)
                                )
                            , cancellationToken);

                            await this.StateManager.SetStateAsync(key, fromDate, cancellationToken);

                            foreach (var uid in uids)
                            {
                                if (this.complaintSeriesDbContext.Ticket.Where(t => t.CodeValue == uid.ToString()).FirstOrDefault() != null)
                                {
                                    this.jobStatusContext.JobStatus.Add(new JobStatusLog()
                                    {
                                        TenantId = "cubik",
                                        Message = string.Format("Skipping item {0}", uid),
                                        CreatedDate = DateTime.Now,
                                    });
                                    continue;
                                }

                                var message = folder.GetMessage(uid, cancellationToken);

                                var body = getBody(message);

                                var addresses = await serviceProxy.CreateServiceProxy<IAddressExtractor>(new Uri("fabric:/TextProcessing/AddressExtractor")).Parse(body);

                                SaveDataToDb(addresses, message, uid, from, body.Trim());
                            }
                        }
                        catch (Exception ex)
                        {
                            this.jobStatusContext.JobStatus.Add(new JobStatusLog()
                            {
                                TenantId = "cubik",
                                Message = string.Format("Job terminated in error: {0}", ex),
                                CreatedDate = DateTime.Now,
                            });
                        }
                        finally
                        {
                            folder.Close(cancellationToken: cancellationToken);
                            this.jobStatusContext.JobStatus.Add(new JobStatusLog()
                            {
                                TenantId = "cubik",
                                Message = string.Format("Ending Job from date {0}", DateTime.Now),
                                CreatedDate = DateTime.Now,
                            });

                            await this.jobStatusContext.SaveChangesAsync(cancellationToken: cancellationToken);
                        }
                    }
                }
                client.Disconnect(true, cancellationToken);
            }
        }

        async void SaveDataToDb(string[] addresses, MimeMessage message, UniqueId uid, string from, string body)
        {
            var dataKey = new DataKeyLocation()
            {
                locationCode = addresses.Length > 0 ? addresses[0] : message.From.FirstOrDefault()!.Name,
                name = string.Format("{0}@{1}", message.From.FirstOrDefault()!.Name, from),
            };
            this.complaintSeriesDbContext.Complaints.Add(
                    new ComplaintSeries()
                    {
                        CreatedDate = message.Date.Date,
                        DataKey = dataKey,
                        NrComanda = dataKey.locationCode,
                        TenantId = "cubik",
                        Status = nrComanda.Match(body).Value,
                        Tickets = new() {
                                                new Ticket()
                                                {
                                                    CodeValue = uid.ToString(),
                                                    Description = body
                                                }
                        }
                    }
                );
            try
            {
                await this.complaintSeriesDbContext.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                this.jobStatusContext.JobStatus.Add(new JobStatusLog()
                {
                    TenantId = "cubik",
                    Message = string.Format("Message {0} terminated in error: {1}", uid, ex),
                    CreatedDate = DateTime.Now,
                });
            }
        }

        async Task IYahooFeederJob.SetOptions(MailSettings settings)
        {
            await StateManager.SetStateAsync("settings", settings);
        }
    }
}