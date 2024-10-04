using System.Fabric;
using System.Text;
using System.Text.RegularExpressions;
using AzureServices;
using AzureTableRepository.MailSettings;
using AzureTableRepository.Tickets;
using MailExtrasExtractor;
using MailKit;
using MailKit.Net.Imap;
using MailKit.Search;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Microsoft.ServiceFabric.Services.Remoting.Client;
using Microsoft.ServiceFabric.Services.Remoting.Runtime;
using Microsoft.ServiceFabric.Services.Remoting.V2.FabricTransport.Client;
using Microsoft.ServiceFabric.Services.Runtime;
using MimeKit;
using RepositoryContract.Tickets;
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
        private static readonly string syncPrefix = "sync_control/ymailtickets${0}";
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
            //ServiceEventSource.Current.ServiceMessage(this.Context, "Service name is {0}. Listen address is {1}", Context.ServiceName.ToString(), Context.ListenAddress);
            await Task.Delay(TimeSpan.FromHours(3));
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();

                await ReadMails(new MailSettings()
                {
                    DaysBefore = int.Parse(Environment.GetEnvironmentVariable("days_before")!),
                    Password = Environment.GetEnvironmentVariable("Password")!,
                    User = Environment.GetEnvironmentVariable("User")!
                }, cancellationToken);

                await Task.Delay(TimeSpan.FromHours(12));
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
            var blob = new BlobAccessStorageService();
            var ticketEntryRepository = new TicketEntryRepository(null);
            var mailSettings = new MailSettingsRepository(null);

            ServiceEventSource.Current.ServiceMessage(Context, "Executing task mails. {0}", DateTime.Now);
            using (ImapClient client = new ImapClient())
            {
                await client.ConnectAsync(
                    "imap.mail.yahoo.com",
                    993,
                    true, cancellationToken); //For SSL
                await client.AuthenticateAsync(settings.User, settings.Password, cancellationToken);
                var mSettings = await mailSettings.GetMailSetting();

                foreach (var setting in mSettings)
                {
                    var meta = blob.GetMetadata(syncPrefix, setting.PartitionKey + setting.RowKey);

                    DateTimeOffset? fromDate = null;
                    if (meta.TryGetValue("sync_data", out var date))
                    {
                        fromDate = DateTimeOffset.Parse(date);
                    }
                    fromDate = fromDate ?? DateTime.Now.AddDays(-settings.DaysBefore);
                    DateTimeOffset? syncDate = fromDate.Value!;

                    foreach (var folder in GetFolders(client, setting.Folders.Split(";", StringSplitOptions.TrimEntries), cancellationToken))
                    {
                        try
                        {
                            var from_entries = setting.From.Split(";", StringSplitOptions.TrimEntries);
                            BinarySearchQuery query = SearchQuery.FromContains(from_entries[0]).Or(SearchQuery.FromContains(from_entries[0]));
                            for (var i = 1; i < from_entries.Length; i++)
                            {
                                query = query.Or(SearchQuery.FromContains(from_entries[i]));
                            }

                            IList<UniqueId> uids;

                            folder.Open(FolderAccess.ReadOnly, cancellationToken);

                            uids = folder.Search(
                                SearchQuery.DeliveredAfter(fromDate.Value.DateTime).And(
                                  query
                                )
                            , cancellationToken);

                            foreach (var messageSummary in await folder.FetchAsync(uids, MessageSummaryItems.UniqueId
                                | MessageSummaryItems.InternalDate
                                | MessageSummaryItems.Envelope
                                | MessageSummaryItems.EmailId
                                | MessageSummaryItems.ThreadId
                                | MessageSummaryItems.References
                                | MessageSummaryItems.BodyStructure)
                                )
                            {
                                try
                                {
                                    if (await ticketEntryRepository.Exists<TicketEntity>(GetPartitionKey(messageSummary), GetRowKey(messageSummary)))
                                        continue;

                                    var ticket = await AddComplaint(messageSummary, ticketEntryRepository);
                                    await SaveAttachments(messageSummary, ticket, ticketEntryRepository);
                                    syncDate = messageSummary.InternalDate > syncDate ? messageSummary.InternalDate : syncDate;
                                }
                                catch (Exception ex)
                                {
                                    ServiceEventSource.Current.ServiceMessage(this.Context, "{0}. {1}", ex.Message, ex.InnerException?.ToString() ?? "");
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            ServiceEventSource.Current.ServiceMessage(this.Context, ex.Message);
                        }
                        finally
                        {
                            folder.Close(cancellationToken: cancellationToken);
                            ServiceEventSource.Current.ServiceMessage(this.Context, "Finished Executing task mails. {0}", DateTime.Now);
                        }
                    }
                    meta["sync_data"] = syncDate.Value.AddDays(-1).ToString();
                    blob.SetMetadata(syncPrefix, null, meta, setting.PartitionKey + setting.RowKey);
                }
                client.Disconnect(true, cancellationToken);
            }
        }

        private async Task<TicketEntity> AddComplaint(IMessageSummary message, ITicketEntryRepository ticketEntryRepository)
        {
            BlobAccessStorageService storageService = new();
            Extras extras = null;
            string contentId = "";

            string file_name = message.UniqueId.ToString();
            string extension = message.HtmlBody != null ? "html" : "txt";
            var fname = $"attachments/{message.Date.Date.ToString("yy")}/{message.UniqueId}/{file_name}.{extension}";

            string body = "";
            if (storageService.Exists(fname))
            {
                body = Encoding.UTF8.GetString(storageService.Access(fname, out var contentType));
            }
            else
            {
                var bodyPart = message.Folder.GetBodyPart(message.UniqueId, message.HtmlBody ?? message.TextBody ?? message.Body);
                using (var stream = new MemoryStream())
                {
                    contentId = bodyPart.ContentId;
                    if (bodyPart is MessagePart)
                    {
                        var rfc822 = (MessagePart)bodyPart;
                        rfc822.Message.WriteTo(stream);
                    }
                    else
                    {
                        var part = (MimePart)bodyPart;
                        part.Content.DecodeTo(stream);
                    }
                    storageService.WriteTo(fname, new BinaryData(stream.ToArray()));
                    stream.Seek(0, SeekOrigin.Begin);
                    body = Encoding.UTF8.GetString(stream.ToArray());
                }
            }

            body = message.HtmlBody != null ? await tokService.HtmlStrip(regexHtml.Replace(body, " ")) : body.Trim() ?? "";
            extras = await serviceProxy.CreateServiceProxy<IMailExtrasExtractor>(new Uri("fabric:/TextProcessing/MailExtrasExtractorType")).Parse(body);

            var ticket = new TicketEntity()
            {
                Sender = message.Envelope.Sender?.FirstOrDefault()?.Name,
                From = string.Join(";", message.Envelope.From?.Select(t => t.Name) ?? []),
                Locations = string.Join(";", extras.Addreses ?? []),
                CreatedDate = message.Date.Date.ToUniversalTime(),
                NrComanda = extras.NumarComanda,
                TicketSource = "Mail",
                PartitionKey = GetPartitionKey(message),
                RowKey = GetRowKey(message),
                InReplyTo = message.Envelope.InReplyTo,
                MessageId = message.Envelope.MessageId,
                References = string.Join(";", message.References),
                Subject = message.NormalizedSubject,
                Description = message.PreviewText ?? body.Substring(0, body.Length - 1 > 256 ? 256 : body.Length - 1),
                ThreadId = message.ThreadId,
                EmailId = message.EmailId,
                ContentId = contentId,
                Uid = (int)message.UniqueId.Id
            };
            ticket.OriginalBodyPath = fname;

            await ticketEntryRepository.Save(ticket);
            return ticket;
        }

        private async Task SaveAttachments(IMessageSummary message, TicketEntity ticket, ITicketEntryRepository ticketEntryRepository)
        {
            BlobAccessStorageService storageService = new();

            if (message.Attachments?.Count() > 0)
            {
                foreach (var attachmentPart in message.Attachments)
                {
                    MimeTypes.TryGetExtension(attachmentPart.ContentType?.MimeType, out var extension);
                    var fName = (attachmentPart.FileName ?? attachmentPart.ContentMd5).Replace("-", "").ToLowerInvariant();
                    var fname = $"attachments/{message.Date.Date.ToString("yy")}/{message.UniqueId}/{fName}.{extension ?? "txt"}";

                    if (!storageService.Exists(fname))
                    {
                        var attachment = message.Folder.GetBodyPart(message.UniqueId, attachmentPart);
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
                            storageService.WriteTo(fname, new BinaryData(stream.ToArray()));
                        }
                    }

                    await ticketEntryRepository.Save(new AttachmentEntry()
                    {
                        PartitionKey = ticket.RowKey,
                        RowKey = Guid.NewGuid().ToString(),
                        Data = fname,
                        ContentType = attachmentPart.ContentType?.MimeType,
                        RefPartition = ticket.PartitionKey,
                        RefKey = ticket.RowKey,
                        ContentId = attachmentPart.ContentId
                    });

                }
            }
        }

        private string GetPartitionKey(IMessageSummary id) => (id.UniqueId.Id.GetHashCode() % 100).ToString();
        private string GetRowKey(IMessageSummary id) => string.IsNullOrEmpty(id.EmailId) ? (id.UniqueId.Id.ToString() + "_" + id.UniqueId.Validity) : id.EmailId;
    }
}
