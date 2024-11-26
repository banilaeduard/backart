using System.Fabric;
using System.Text;
using AzureServices;
using AzureTableRepository.DataKeyLocation;
using AzureTableRepository.MailSettings;
using AzureTableRepository.Tickets;
using EntityDto;
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
using RepositoryContract;
using RepositoryContract.DataKeyLocation;
using RepositoryContract.Tickets;
using V2.Interfaces;
using YahooFeeder;
using UniqueId = MailKit.UniqueId;

namespace YahooTFeeder
{
    /// <summary>
    /// An instance of this class is created for each service instance by the Service Fabric runtime.
    /// </summary>
    internal sealed class YahooTFeeder : StatelessService, IYahooFeeder
    {
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
            await Task.Delay(TimeSpan.FromHours(1), cancellationToken);
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();

                await ReadMails(new MailSettings()
                {
                    DaysBefore = int.Parse(Environment.GetEnvironmentVariable("days_before")!),
                    Password = Environment.GetEnvironmentVariable("Password")!,
                    User = Environment.GetEnvironmentVariable("User")!
                }, cancellationToken);

                cancellationToken.ThrowIfCancellationRequested();
                await Task.Delay(TimeSpan.FromHours(3), cancellationToken);
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
            var tableStorageService = new TableStorageService(null);
            var ticketEntryRepository = new TicketEntryRepository(null);
            var mailSettings = new MailSettingsRepository(null);

            var mSettings = (await mailSettings.GetMailSetting()).ToList();
            var settingMap = mSettings
                .SelectMany(x => x.From.Split(";", StringSplitOptions.TrimEntries))
                .Distinct()
                .ToDictionary(x => x, v => mSettings.First(x => x.From.Contains(v)));
            var folderRecipients = mSettings
                .SelectMany(x => x.Folders.Split(";", StringSplitOptions.TrimEntries))
                .GroupBy(x => x)
                .OrderByDescending(x => x.Count())
                .Select(x => x.First())
                .ToDictionary(x => x, v => mSettings.Where(x => x.Folders.Contains(v))
                                                    .SelectMany(x => x.From.Split(";", StringSplitOptions.TrimEntries))
                                                    .Distinct()
                                                    .Order()
                                                    .ToList());

            ServiceEventSource.Current.ServiceMessage(Context, "Executing task mails. {0}", DateTime.Now);
            using (ImapClient client = await ConnectAsync(settings, cancellationToken))
            {
                foreach (var (folderName, recipientsList) in folderRecipients)
                {
                    var fromDate = DateTime.Now.AddDays(-settings.DaysBefore).ToUniversalTime();
                    var lastRun = tableStorageService.Query<MailEntryStatus>(t => t.PartitionKey == folderName).ToList();
                    var startTimer = DateTime.Now.ToUniversalTime();

                    foreach (var batchRecipients in recipientsList.Chunk(10))
                    {
                        var defaultTimers = batchRecipients.ToDictionary(x => x, v => lastRun.FirstOrDefault(r => r.From == v)?.LastFetch ?? fromDate);
                        var groupedTimers = defaultTimers.GroupBy(x => x.Value).ToDictionary(x => x.Key, v => v.Select(t => t.Key).ToList());
                        IMailFolder folder = null;

                        try
                        {
                            SearchQuery query = null;

                            foreach (var item in groupedTimers)
                            {
                                SearchQuery qRecipients = SearchQuery.FromContains(item.Value[0])
                                .Or(SearchQuery.CcContains(item.Value[0]))
                                .Or(SearchQuery.ToContains(item.Value[0]));
                                for (var i = 1; i < item.Value.Count(); i++)
                                {
                                    qRecipients = qRecipients.Or(SearchQuery.FromContains(item.Value[i])
                                    .Or(SearchQuery.CcContains(item.Value[i]))
                                    .Or(SearchQuery.ToContains(item.Value[i])));
                                }

                                query = query != null ? query.Or(SearchQuery.DeliveredAfter(item.Key.AddDays(-1)).And(qRecipients)) : SearchQuery.DeliveredAfter(item.Key.AddDays(-1)).And(qRecipients);
                            }

                            List<UniqueId> uids;

                            folder = GetFolders(client, [folderName], cancellationToken).ElementAt(0);
                            folder.Open(FolderAccess.ReadOnly, cancellationToken);

                            uids = folder.Search(query, cancellationToken).ToList();

                            List<IMessageSummary> toProcess = new();
                            if (uids?.Any() == true)
                                foreach (var messageSummary in await folder.FetchAsync(uids, MessageSummaryItems.InternalDate
                                    | MessageSummaryItems.EmailId
                                    | MessageSummaryItems.UniqueId))
                                {
                                    if (await ticketEntryRepository.GetIfExists<TicketEntity>(GetPartitionKey(messageSummary), GetRowKey(messageSummary)) != null)
                                        continue;

                                    toProcess.Add(messageSummary);
                                }
                            if (toProcess.Count > 0)
                            {
                                await AddComplaint(toProcess, ticketEntryRepository, folder);
                            }

                            var batchRun = lastRun.Where(x => batchRecipients.Contains(x.From)).ToList();

                            if (batchRun.Count != batchRecipients.Count())
                            {
                                foreach (var recipient in batchRecipients.Except(batchRun.Select(x => x.From)))
                                {
                                    batchRun.Add(new MailEntryStatus()
                                    {
                                        PartitionKey = folderName,
                                        RowKey = recipient,
                                        LastFetch = startTimer,
                                        Folder = folderName,
                                        From = recipient,
                                    });
                                }
                            }

                            foreach (var status in batchRun)
                            {
                                status.LastFetch = startTimer;
                            }
                            await tableStorageService.PrepareUpsert(batchRun).ExecuteBatch();
                        }
                        catch (Exception ex)
                        {
                            LogError(ex);
                        }
                        finally
                        {
                        }
                    }
                }
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

                string extension = message.HtmlBody != null ? "html" : "txt";
                var fname = $"attachments/{GetPartitionKey(message)}/{GetRowKey(message)}/body.{extension}";

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
                            var bodyPart = message.Folder.GetBodyPart(message.UniqueId, message.Body);
                            var bodyVisitor = new HtmlPreviewVisitor();
                            bodyPart.Accept(bodyVisitor);
                            using (var stream = new MemoryStream())
                            {
                                storageService.WriteTo(fname, new BinaryData(Encoding.UTF8.GetBytes(bodyVisitor.HtmlBody)));
                                body = bodyVisitor.HtmlBody;
                            }
                            body = body.Trim().Replace("__", "") ?? "";
                            extras = await serviceProxy.CreateServiceProxy<IMailExtrasExtractor>(new Uri("fabric:/TextProcessing/MailExtrasExtractorType")).Parse(body);
                            body = body.Length > 2048 ? body.Substring(0, 2048) : body;
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
                            TownName = string.Join(";", extras?.Addreses ?? froms ?? []),
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
                    Uid = Convert.ToInt32(message.UniqueId.Id),
                    Validity = Convert.ToInt32(message.UniqueId.Validity),
                    OriginalBodyPath = fname,
                    LocationCode = location?.LocationCode,
                    LocationRowKey = location?.RowKey,
                    LocationPartitionKey = location?.PartitionKey,
                    HasAttachments = message.Attachments?.Any() == true,
                    FoundInFolder = message.Folder.Name,
                    CurrentFolder = message.Folder.Name
                });
            }
            await ticketEntryRepository.Save([.. toSave]);
            var processQueue = await QueueService.GetClient("addmailtotask");
            processQueue.SendMessage(QueueService.Serialize(toSave.Select(t => new AddMailToTask()
            {
                PartitionKey = t.PartitionKey,
                RowKey = t.RowKey,
                ThreadId = t.ThreadId,
                Date = t.CreatedDate,
                TableName = nameof(TicketEntity)
            })));
        }
        private string GetPartitionKey(IMessageSummary id) => id.UniqueId.Validity.ToString();
        private string GetRowKey(IMessageSummary id) => id.UniqueId.Id.ToString();
        private void LogError(Exception ex)
        {
            ServiceEventSource.Current.ServiceMessage(Context, "{0}. Stack trace: {1}", ex.Message, ex.StackTrace ?? "");
        }

        public async Task<MailBody[]> DownloadAll(TableEntityPK[] uids)
        {
            var mailSettings = new MailSettingsRepository(null);
            var ticketEntryRepository = new TicketEntryRepository(null);
            var blob = new BlobAccessStorageService();
            List<MailBody> result = new();

            try
            {
                var attachments = await ticketEntryRepository.GetAllAttachments();
                attachments = [.. attachments.Where(t => uids.Any(u => u.RowKey == t.RefKey && u.PartitionKey == t.RefPartition))];
                foreach (var uid in uids)
                {
                    var fname = $"attachments/{uid.PartitionKey}/{uid.RowKey}/body.eml";

                    if (attachments.Any(a => a.RefPartition == uid.PartitionKey && a.RefKey == uid.RowKey))
                    {
                        result.Add(new() { TableEntity = uid, Path = fname });
                        continue;
                    }
                }

                if (result.Count == uids.Count()) return result.ToArray();

                var missingUids = uids.Except(result.Select(t => t.TableEntity));
                var tickets = (await ticketEntryRepository.GetAll()).Where(t => missingUids.Any(u => u.PartitionKey == t.PartitionKey && u.RowKey == t.RowKey));
                var allFolders = (await mailSettings.GetMailSetting()).SelectMany(t => t.Folders.Split(";", StringSplitOptions.TrimEntries)).Distinct().ToArray();

                var foundIn = tickets.Where(x => !string.IsNullOrEmpty(x.CurrentFolder)).Select(x => x.CurrentFolder)
                                    .Distinct()
                                    .ToList();

                using (var client = await ConnectAsync(new MailSettings()
                {
                    DaysBefore = int.Parse(Environment.GetEnvironmentVariable("days_before")!),
                    Password = Environment.GetEnvironmentVariable("Password")!,
                    User = Environment.GetEnvironmentVariable("User")!
                }, CancellationToken.None))
                {
                    foreach (var folder in GetFolders(client, [.. foundIn.Concat(allFolders.Except(foundIn).ToArray())], CancellationToken.None))
                    {
                        var uidsMissing = tickets.Select(t => new UniqueId((uint)t.Validity, (uint)t.Uid)).ToList();
                        if (uidsMissing.Count == 0) return result.ToArray();
                        await folder.OpenAsync(FolderAccess.ReadOnly);
                        var found = folder.Search(SearchQuery.Uids(uidsMissing));

                        foreach (var uid in found)
                        {
                            var entry = tickets.First(t => uid.Validity == t.Validity && uid.Id == t.Uid);
                            var msg = folder.GetMessage(uid);
                            HtmlPreviewVisitor visitor = new();
                            msg.Accept(visitor);

                            var fname = $"attachments/{entry.PartitionKey}/{entry.RowKey}/body.eml";
                            var details = $"attachments/{entry.PartitionKey}/{entry.RowKey}/details/";
                            if (!blob.Exists(fname))
                                using (var ms = new MemoryStream())
                                {
                                    msg.WriteTo(FormatOptions.Default, ms);
                                    blob.WriteTo(fname, new BinaryData(ms.ToArray()));
                                }

                            await ticketEntryRepository.Save(new AttachmentEntry()
                            {
                                PartitionKey = entry.Uid.ToString(),
                                RowKey = entry.Validity + "eml",
                                Data = fname,
                                ContentType = "eml",
                                Title = "body.eml",
                                RefPartition = entry.PartitionKey,
                                RefKey = entry.RowKey,
                            });
                            if (!blob.Exists(details + "body.html"))
                                blob.WriteTo(details + "body.html", new BinaryData(visitor.HtmlBody));
                            await ticketEntryRepository.Save(new AttachmentEntry()
                            {
                                PartitionKey = entry.Uid.ToString(),
                                RowKey = entry.Validity + "body",
                                Data = details + "body.html",
                                Title = "body.html",
                                ContentType = "html",
                                RefPartition = entry.PartitionKey,
                                RefKey = entry.RowKey,
                            });

                            int idx = 0;
                            foreach (var attachment in visitor.Attachments)
                            {
                                var fileName = attachment.ContentDisposition?.FileName ?? attachment.ContentType?.Name ?? Guid.NewGuid().ToString().Replace("-", "");
                                var filePath = details + idx + fileName;
                                if (!blob.Exists(filePath))
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
                                        blob.WriteTo(filePath, new BinaryData(stream.ToArray()));
                                    }

                                await ticketEntryRepository.Save(new AttachmentEntry()
                                {
                                    PartitionKey = entry.Uid.ToString(),
                                    RowKey = Guid.NewGuid().ToString(),
                                    Data = filePath,
                                    Title = fileName,
                                    ContentType = attachment.ContentType?.MimeType,
                                    RefPartition = entry.PartitionKey,
                                    RefKey = entry.RowKey,
                                    ContentId = attachment.ContentId
                                });

                                idx++;
                            }
                            var uu = missingUids.First(t => t.PartitionKey == entry.PartitionKey && t.RowKey == entry.RowKey);
                            result.Add(new MailBody() { TableEntity = uu, Path = fname });
                        }
                        tickets = tickets.ExceptBy(found, t => new UniqueId((uint)t.Validity, (uint)t.Uid));
                    }
                }
            }
            catch (Exception ex)
            {
                LogError(ex);
                throw;
            }

            return result.ToArray();
        }

        public async Task Move(MoveToMessage<TableEntityPK>[] messages)
        {
            var mailSettings = new MailSettingsRepository(null);
            var ticketEntryRepository = new TicketEntryRepository(null);

            var allFolders = (await mailSettings.GetMailSetting()).SelectMany(t => t.Folders.Split(";", StringSplitOptions.TrimEntries))
                .Distinct()
                .ToArray();
            var ticket = await ticketEntryRepository.GetAll();

            using (var client = await ConnectAsync(new MailSettings()
            {
                DaysBefore = int.Parse(Environment.GetEnvironmentVariable("days_before")!),
                Password = Environment.GetEnvironmentVariable("Password")!,
                User = Environment.GetEnvironmentVariable("User")!
            }, CancellationToken.None))
            {
                foreach (var msg in messages)
                {
                    var destinationFolder = GetFolders(client, [msg.DestinationFolder], CancellationToken.None).ElementAt(0);

                    var entries = ticket.Where(x => msg.Items.Any(f => f.RowKey == x.RowKey && f.PartitionKey == x.PartitionKey)).ToList();
                    var foundIn = entries.Where(x => !string.IsNullOrEmpty(x.CurrentFolder))
                                        .Select(x => x.CurrentFolder)
                                        .Distinct()
                                        .ToList();
                    allFolders = [.. foundIn.Concat(allFolders.Except(foundIn))];
                    var uids = entries.Select(x => new UniqueId((uint)x.Validity, (uint)x.Uid)).ToList();
                    
                    foreach (var folder in GetFolders(client, allFolders, CancellationToken.None))
                    {
                        if (folder.Name == destinationFolder.Name) continue;
                        if (!uids.Any()) break;

                        await folder.OpenAsync(FolderAccess.ReadWrite);
                        var found = await folder.SearchAsync(SearchQuery.Uids(uids));
                        if (found.Any())
                        {
                            await folder.MoveToAsync(uids, destinationFolder, CancellationToken.None);

                            var moved = entries.Where(x => found.Any(f => f.Validity == x.Validity && f.Id == x.Uid)).ToList();
                            foreach (var x in moved)
                            {
                                x.CurrentFolder = msg.DestinationFolder;
                                if (string.IsNullOrEmpty(x.FoundInFolder))
                                {
                                    x.FoundInFolder = folder.Name;
                                }
                            }
                            await ticketEntryRepository.Save([.. moved]);

                            uids = [.. uids.Except(found)];
                        }
                    }
                }
            }
        }

        private async Task<ImapClient> ConnectAsync(MailSettings settings, CancellationToken cancellationToken)
        {
            ImapClient client = new ImapClient();
            client.ServerCertificateValidationCallback = (s, c, h, e) => true;
            await client.ConnectAsync("imap.mail.yahoo.com", 993, true, cancellationToken);
            await client.AuthenticateAsync(settings.User, settings.Password, cancellationToken);
            return client;
        }
    }
}
