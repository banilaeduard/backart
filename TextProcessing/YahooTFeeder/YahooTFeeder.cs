using System.Fabric;
using System.Text;
using AzureServices;
using AzureTableRepository.DataKeyLocation;
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
using RepositoryContract.DataKeyLocation;
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

                            BinarySearchQuery query2 = SearchQuery.ToContains(from_entries[0]).Or(SearchQuery.ToContains(from_entries[0]));
                            for (var i = 1; i < from_entries.Length; i++)
                            {
                                query2 = query2.Or(SearchQuery.ToContains(from_entries[i]));
                            }

                            List<UniqueId> uids;

                            folder.Open(FolderAccess.ReadOnly, cancellationToken);

                            uids = folder.Search(
                                SearchQuery.DeliveredAfter(fromDate.Value.DateTime).And(
                                  query
                                )
                            , cancellationToken).ToList();

                            var uids2 = folder.Search(
                                SearchQuery.DeliveredAfter(fromDate.Value.DateTime).And(
                                  SearchQuery.FromContains(settings.User).And(query2)
                                )
                            , cancellationToken);

                            uids.AddRange(uids2);

                            List<IMessageSummary> toProcess = new();
                            if (uids?.Any() == true)
                                foreach (var messageSummary in await folder.FetchAsync(uids, MessageSummaryItems.InternalDate | MessageSummaryItems.EmailId | MessageSummaryItems.UniqueId))
                                {
                                    if (await ticketEntryRepository.Exists<TicketEntity>(GetPartitionKey(messageSummary), GetRowKey(messageSummary)))
                                        continue;

                                    toProcess.Add(messageSummary);
                                }
                            if (toProcess.Count > 0)
                            {
                                await AddComplaint(toProcess, ticketEntryRepository, folder);
                                var sample = toProcess.OrderByDescending(t => t.InternalDate).First().InternalDate;
                                //await SaveAttachments(messageSummary, ticket, ticketEntryRepository);
                                syncDate = sample > syncDate ? sample : syncDate;
                            }
                        }
                        catch (Exception ex)
                        {
                            LogError(ex);
                        }
                        finally
                        {
                            folder.Close(cancellationToken: cancellationToken);
                            ServiceEventSource.Current.ServiceMessage(Context, "Finished Executing task mails. {0}", DateTime.Now);
                        }
                    }
                    meta["sync_data"] = syncDate.Value.ToString();
                    blob.SetMetadata(syncPrefix, null, meta, setting.PartitionKey + setting.RowKey);
                }
                client.Disconnect(true, cancellationToken);
            }
        }

        private async Task AddComplaint(IList<IMessageSummary> messages, ITicketEntryRepository ticketEntryRepository, IMailFolder folder)
        {
            BlobAccessStorageService storageService = new();
            DataKeyLocationRepository locationRepository = new(null);
            messages = await folder.FetchAsync([.. messages.Select(t => t.UniqueId)],
                         MessageSummaryItems.UniqueId
                                        | MessageSummaryItems.InternalDate
                                        | MessageSummaryItems.Envelope
                                        | MessageSummaryItems.EmailId
                                        | MessageSummaryItems.ThreadId
                                        | MessageSummaryItems.References
                                        | MessageSummaryItems.BodyStructure
                                        | MessageSummaryItems.PreviewText);
            List<TicketEntity> toSave = [];

            foreach (var message in messages)
            {
                Extras extras = null;
                string contentId = "";

                string file_name = message.UniqueId.Id + "_" + message.UniqueId.Validity;
                string extension = message.HtmlBody != null ? "html" : "txt";
                var fname = $"attachments/{message.Date.Date.ToString("yy")}/{file_name}/{file_name}.{extension}";

                string body = "";
                try
                {
                    if (storageService.Exists(fname))
                    {
                        body = Encoding.UTF8.GetString(storageService.Access(fname, out var contentType));
                        extras = await serviceProxy.CreateServiceProxy<IMailExtrasExtractor>(new Uri("fabric:/TextProcessing/MailExtrasExtractorType")).Parse(body);
                        body = body.Length > 512 ? body.Substring(0, 512) : body;
                    }
                    else
                    {
                        if (!string.IsNullOrEmpty(message.PreviewText))
                        {
                            using (var stream = new MemoryStream())
                            {
                                storageService.WriteTo(fname, new BinaryData(Encoding.UTF8.GetBytes(message.PreviewText)));
                                body = message.PreviewText;
                                extras = await serviceProxy.CreateServiceProxy<IMailExtrasExtractor>(new Uri("fabric:/TextProcessing/MailExtrasExtractorType")).Parse(body);
                            }
                        }
                        else
                        {
                            var bodyPart = message.Folder.GetBodyPart(message.UniqueId, message.TextBody ?? message.HtmlBody ?? message.Body);
                            using (var stream = new MemoryStream())
                            {
                                if (bodyPart is MultipartAlternative)
                                {
                                    bodyPart = ((MultipartAlternative)bodyPart).Last();
                                }
                                else if (bodyPart is Multipart)
                                {
                                    bodyPart = ((Multipart)bodyPart).Where(t => !t.IsAttachment).FirstOrDefault();
                                }
                                if (bodyPart == null) break;

                                contentId = bodyPart.ContentId;
                                if (bodyPart is MessagePart)
                                {
                                    var rfc822 = (MessagePart)bodyPart;
                                    rfc822.Message.WriteTo(stream);
                                }
                                else
                                {
                                    var part = (MimePart)bodyPart;
                                    part.Content?.DecodeTo(stream);
                                }
                                storageService.WriteTo(fname, new BinaryData(stream.ToArray()));
                                stream.Seek(0, SeekOrigin.Begin);
                                body = Encoding.UTF8.GetString(stream.ToArray());
                            }
                            body = body.Trim().Replace("__", "") ?? "";
                            extras = await serviceProxy.CreateServiceProxy<IMailExtrasExtractor>(new Uri("fabric:/TextProcessing/MailExtrasExtractorType")).Parse(body);
                            body = body.Length > 512 ? body.Substring(0, 512) : body;
                        }
                    }
                }
                catch (Exception ex)
                {
                    LogError(ex);
                    body = "Error";
                }

                var froms = message.Envelope.From?.Select(t => t.ToString()).ToArray();
                DataKeyLocationEntry? location = null;
                DataKeyLocationEntry? locationMain = null;
                if (froms?.Any() == true)
                {
                    var locations = await locationRepository.GetLocations();
                    location = locations.FirstOrDefault(loc => froms.Any(frm => loc.LocationName == frm));
                    if (location == null)
                    {
                        location = (await locationRepository.InsertLocation([new DataKeyLocationEntry()
                        {
                            LocationName = froms[0],
                            LocationCode = froms[0],
                            TownName = string.Join(";", extras.Addreses ?? froms ?? []),
                        }]))[0];
                    }
                    else if ((locationMain = locations.FirstOrDefault(loc => loc.LocationCode == location.LocationCode && loc.MainLocation)) != null)
                    {
                        location = locationMain;
                    }
                }

                toSave.Add(new TicketEntity()
                {
                    Sender = message.Envelope.Sender?.FirstOrDefault()?.ToString(),
                    From = string.Join(";", message.Envelope.From?.Select(t => t.ToString()) ?? []),
                    Locations = string.Join(";", extras?.Addreses ?? []),
                    CreatedDate = message.Date.Date.ToUniversalTime(),
                    NrComanda = extras?.NumarComanda,
                    TicketSource = "Mail",
                    PartitionKey = GetPartitionKey(message),
                    RowKey = GetRowKey(message),
                    InReplyTo = message.Envelope.InReplyTo,
                    MessageId = message.Envelope.MessageId,
                    References = string.Join(";", message.References),
                    Subject = message.NormalizedSubject,
                    Description = body,
                    ThreadId = message.ThreadId + "_" + message.UniqueId.Validity,
                    EmailId = message.EmailId,
                    ContentId = contentId,
                    Uid = message.UniqueId.Id + "_" + message.UniqueId.Validity,
                    OriginalBodyPath = fname,
                    LocationCode = location?.LocationCode,
                    LocationRowKey = location?.RowKey,
                    LocationPartitionKey = location?.PartitionKey
                });
            }
            await ticketEntryRepository.Save([.. toSave]);
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

        private string GetPartitionKey(IMessageSummary id) => id.InternalDate.Value.ToString("yyMM");
        private string GetRowKey(IMessageSummary id) => string.IsNullOrEmpty(id.EmailId) ? (id.UniqueId.Id.ToString() + "_" + id.UniqueId.Validity) : id.EmailId;
        private void LogError(Exception ex)
        {
            ServiceEventSource.Current.ServiceMessage(Context, "{0}. Stack trace: {1}", ex.Message, ex.StackTrace ?? "");
        }
    }
}
