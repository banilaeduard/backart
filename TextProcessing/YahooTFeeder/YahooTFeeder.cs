using System.Fabric;
using System.Text.RegularExpressions;
using DataAccess;
using DataAccess.Context;
using DataAccess.Entities;
using MailExtrasExtractor;
using MailKit;
using MailKit.Net.Imap;
using MailKit.Search;
using Microsoft.EntityFrameworkCore;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Microsoft.ServiceFabric.Services.Remoting.Client;
using Microsoft.ServiceFabric.Services.Remoting.Runtime;
using Microsoft.ServiceFabric.Services.Remoting.V2.FabricTransport.Client;
using Microsoft.ServiceFabric.Services.Remoting.V2.FabricTransport.Runtime;
using Microsoft.ServiceFabric.Services.Runtime;
using MimeKit;
using Tokenizer;
using YahooFeeder;
using UniqueId = MailKit.UniqueId;

namespace YahooTFeeder
{
    /// <summary>
    /// An instance of this class is created for each service instance by the Service Fabric runtime.
    /// </summary>
    internal sealed class YahooTFeeder : StatelessService, IYahooFeeder
    {
        private static Regex regexHtml = new Regex(@"(<br />|<br/>|</ br>|</br>)|<br>");
        private static readonly TokenizerService tokService = new();
        private ServiceProxyFactory serviceProxy;
        public YahooTFeeder(StatelessServiceContext context)
            : base(context)
        {
            serviceProxy = new ServiceProxyFactory((c) =>
            {
                return new FabricTransportServiceRemotingClientFactory();
            });
        }

        /// <summary>
        /// Optional override to create listeners (e.g., TCP, HTTP) for this service replica to handle client or user requests.
        /// </summary>
        /// <returns>A collection of listeners.</returns>
        protected override IEnumerable<ServiceInstanceListener> CreateServiceInstanceListeners()
        {
            return this.CreateServiceRemotingInstanceListeners();
        }


        public async Task Get()
        {
            await ReadMails(new MailSettings()
            {
                Folders = Environment.GetEnvironmentVariable("y_folders")!.Split(";", StringSplitOptions.TrimEntries),
                From = Environment.GetEnvironmentVariable("y_from")!.Split(";", StringSplitOptions.TrimEntries),
                DaysBefore = int.Parse(Environment.GetEnvironmentVariable("days_before")!),
                Password = Environment.GetEnvironmentVariable("Password")!,
                User = Environment.GetEnvironmentVariable("User")!
            }, CancellationToken.None);
        }

        /// <summary>
        /// This is the main entry point for your service instance.
        /// </summary>
        /// <param name="cancellationToken">Canceled when Service Fabric needs to shut down this service instance.</param>
        protected override async Task RunAsync(CancellationToken cancellationToken)
        {
            // TODO: Replace the following sample code with your own logic 
            //       or remove this RunAsync override if it's not needed in your service.
            ServiceEventSource.Current.ServiceMessage(this.Context, "Service name is {0}. Listen address is {1}", Context.ServiceName.ToString(), Context.ListenAddress);
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();

                await ReadMails(new MailSettings()
                {
                    Folders = Environment.GetEnvironmentVariable("y_folders")!.Split(";", StringSplitOptions.TrimEntries),
                    From = Environment.GetEnvironmentVariable("y_from")!.Split(";", StringSplitOptions.TrimEntries),
                    DaysBefore = int.Parse(Environment.GetEnvironmentVariable("days_before")!),
                    Password = Environment.GetEnvironmentVariable("Password")!,
                    User = Environment.GetEnvironmentVariable("User")!
                }, cancellationToken);

                await Task.Delay(TimeSpan.FromHours(3));
            }
        }

        IEnumerable<IMailFolder> GetFolders(ImapClient client, string[] folders, CancellationToken cancellationToken)
        {
            var personal = client.GetFolder(client.PersonalNamespaces[0]);

            foreach (var folder in folders)
            {
                yield return personal.GetSubfolder(folder, cancellationToken);
            }
        }

        async Task<string> getBody(MimeMessage message)
        {
            return !string.IsNullOrWhiteSpace(message.HtmlBody) ?
                                                    await tokService.HtmlStrip(regexHtml.Replace(message.HtmlBody, " ")) : message.TextBody.Trim() ?? "";
        }

        async Task ReadMails(MailSettings settings, CancellationToken cancellationToken)
        {
            var complaintSeriesDbContext = DbContextFactory.GetContext<ComplaintSeriesDbContext>(Environment.GetEnvironmentVariable("ConnectionString"), new NoFilterBaseContext());
            var jobStatusContext = DbContextFactory.GetContext<JobStatusContext>(Environment.GetEnvironmentVariable("ConnectionString"), new NoFilterBaseContext());

            using (ImapClient client = new ImapClient())
            {
                await client.ConnectAsync(
                    "imap.mail.yahoo.com",
                    993,
                    true, cancellationToken); //For SSL
                await client.AuthenticateAsync(settings.User, settings.Password, cancellationToken);
                jobStatusContext.JobStatus.Add(new JobStatusLog()
                {
                    TenantId = "cubik",
                    Message = string.Format("Started Job at date {0} ", DateTime.Now),
                    CreatedDate = DateTime.Now,
                });

                foreach (var folder in GetFolders(client, settings.Folders, cancellationToken))
                {
                    foreach (var from in settings.From)
                    {
                        try
                        {
                            IList<UniqueId> uids;
                            DateTime fromDate = DateTime.Now.AddDays(-settings.DaysBefore);

                            jobStatusContext.JobStatus.Add(new JobStatusLog()
                            {
                                TenantId = "cubik",
                                Message = string.Format("Executing for {0}_{1} @ {2}", from, folder, fromDate),
                                CreatedDate = DateTime.Now,
                            });

                            folder.Open(FolderAccess.ReadOnly, cancellationToken);

                            uids = folder.Search(
                                SearchQuery.DeliveredAfter(fromDate).And(
                                  SearchQuery.FromContains(from)
                                )
                            , cancellationToken);

                            foreach (var uid in uids)
                            {
                                if (complaintSeriesDbContext.Ticket.Where(t => t.CodeValue == uid.ToString()).FirstOrDefault() != null)
                                {
                                    continue;
                                }

                                var message = folder.GetMessage(uid, cancellationToken);

                                var body = await getBody(message);

                                var extras = await serviceProxy.CreateServiceProxy<IMailExtrasExtractor>(new Uri("fabric:/TextProcessing/MailExtrasExtractorType")).Parse(body);

                                var complaint = AddComplaint(message, extras, from, uid, complaintSeriesDbContext);
                                await SaveAttachments(message, complaint.Tickets[0], complaintSeriesDbContext);

                                try
                                {
                                    await complaintSeriesDbContext.SaveChangesAsync();
                                }
                                catch (Exception ex)
                                {
                                    ServiceEventSource.Current.ServiceMessage(this.Context, "{0}. {1}", ex.Message, ex.InnerException?.ToString() ?? "");
                                    jobStatusContext.JobStatus.Add(new JobStatusLog()
                                    {
                                        TenantId = "cubik",
                                        Message = string.Format("Message {0} terminated in error: {1}", uid, ex),
                                        CreatedDate = DateTime.Now,
                                    });
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            ServiceEventSource.Current.ServiceMessage(this.Context, ex.Message);
                            jobStatusContext.JobStatus.Add(new JobStatusLog()
                            {
                                TenantId = "cubik",
                                Message = string.Format("Job terminated in error: {0}", ex),
                                CreatedDate = DateTime.Now,
                            });
                        }
                        finally
                        {
                            folder.Close(cancellationToken: cancellationToken);
                            jobStatusContext.JobStatus.Add(new JobStatusLog()
                            {
                                TenantId = "cubik",
                                Message = string.Format("Ending job at {0}", DateTime.Now),
                                CreatedDate = DateTime.Now,
                            });
                        }
                    }
                }
                await jobStatusContext.SaveChangesAsync(cancellationToken: cancellationToken);
                client.Disconnect(true, cancellationToken);
            }
        }

        private ComplaintSeries AddComplaint(MimeMessage message, Extras extras, string from, UniqueId uid, ComplaintSeriesDbContext complaintSeriesDbContext)
        {
            var dataKey = new DataKeyLocation()
            {
                locationCode = extras.Addreses.Length > 0 ? extras.Addreses[0] : message.From.FirstOrDefault()!.Name,
                name = string.Format("{0}@{1}", message.From.FirstOrDefault()!.Name, from),
            };

            dataKey = complaintSeriesDbContext.DataKeyLocation.Where(t => t.name == dataKey.name).FirstOrDefault() ?? dataKey;

            return complaintSeriesDbContext.Complaints.Add(
                                        new ComplaintSeries()
                                        {
                                            CreatedDate = message.Date.Date,
                                            DataKey = dataKey,
                                            NrComanda = extras.NumarComanda,
                                            TenantId = "cubik",
                                            Status = message.Subject,
                                            Tickets = [
                                                new Ticket()
                                                {
                                                    CodeValue = uid.ToString(),
                                                    Description = extras.BodyResult
                                                }
                                            ]
                                        }
                                    ).Entity;
        }

        private async Task SaveAttachments(MimeMessage message, Ticket ticket, ComplaintSeriesDbContext complaintSeriesDbContext)
        {
            if (message.Attachments?.Count() > 0)
            {
                foreach (var attachment in message.Attachments)
                {
                    using (var stream = new MemoryStream())
                    {
                        if (attachment is MessagePart)
                        {
                            var part = (MessagePart)attachment;
                            await part.Message.WriteToAsync(stream);
                        }
                        else
                        {
                            var part = (MimePart)attachment;
                            await part.Content.DecodeToAsync(stream);
                        }
                        var img = new Attachment()
                        {
                            Data = string.Format("data:{0};base64,{1}", attachment.ContentType.MimeType, Convert.ToBase64String(stream.ToArray())),
                            Ticket = ticket,
                            CreatedDate = new DateTime(message.Date.Ticks),
                            UpdatedDate = new DateTime(message.ResentDate.Ticks),
                            ContentType = attachment.ContentType.MimeType,
                        };
                        complaintSeriesDbContext.Entry(img).State = EntityState.Added;
                    }
                }
            }
        }
    }
}
