using System.Text;
using AzureFabricServices;
using AzureServices;
using AzureTableRepository.DataKeyLocation;
using AzureTableRepository.MailSettings;
using AzureTableRepository.Tickets;
using EntityDto;
using MailKit;
using MailKit.Net.Imap;
using MailKit.Search;
using MailReader;
using Microsoft.ServiceFabric.Actors.Runtime;
using MimeKit;
using RepositoryContract;
using RepositoryContract.DataKeyLocation;
using RepositoryContract.MailSettings;
using RepositoryContract.Tickets;
using ServiceImplementation;
using ServiceImplementation.Caching;
using ServiceInterface.Storage;
using UniqueId = MailKit.UniqueId;

namespace YahooTFeeder
{
    internal enum Operation
    {
        Fetch = 1,
        Download = 2,
        Move = 4,
        All = 7
    }

    internal class YahooTFeeder
    {
        internal static ActorEventSource logger;
        internal static Actor actor;

        internal static async Task Batch(string sourceName, Operation op,
            TableEntityPK[] download_uids, MoveToMessage<TableEntityPK>[] messages,
            CancellationToken cancellationToken)
        {
            var mailSettings = new MailSettingsRepository();
            IMetadataService metadataService = new FabricMetadataService();
            var ticketEntryRepository = new TicketEntryRepository(
                new AlwaysGetCacheManager<TicketEntity>(metadataService), 
                new AlwaysGetCacheManager<AttachmentEntry>(metadataService));
            IList<TicketEntity> tickets = null;
            IList<AttachmentEntry> attachments = null;
            Dictionary<string, List<string>> folderRecipients = null;

            bool downlaod = (op & Operation.Download) == Operation.Download;
            bool move = (op & Operation.Move) == Operation.Move;
            bool fetch = (op & Operation.Fetch) == Operation.Fetch;

            var settings = (await mailSettings.GetMailSource()).FirstOrDefault(t => t.PartitionKey == sourceName);
            if(settings == null)
            {
                logger.ActorMessage(actor, "No settings for {0}. Cannot run the mail service", sourceName);
                return;
            }
            var mSettings = (await mailSettings.GetMailSetting(settings.PartitionKey)).ToList();

            if (downlaod || move)
            {
                tickets = await ticketEntryRepository.GetAll();
                if (downlaod)
                    attachments = await ticketEntryRepository.GetAllAttachments();
            }

            if (fetch)
            {
                folderRecipients = mSettings
                    .SelectMany(x => x.Folders.Split(";", StringSplitOptions.TrimEntries))
                    .GroupBy(x => x)
                    .OrderByDescending(x => x.Count())
                    .Select(x => x.First())
                    .ToDictionary(x => x, v => mSettings.Where(x => x.Folders.Contains(v))
                                                        .SelectMany(x => x.From.Split(";", StringSplitOptions.TrimEntries))
                                                        .Distinct()
                                                        .Order()
                                                        .ToList());
            }

            using (ImapClient client = await ConnectAsync(settings, cancellationToken))
            {
                if (downlaod)
                {
                    await DownloadAll(client, mSettings, tickets!, attachments!, download_uids);
                }
                if (move)
                {
                    await Move(client, mSettings, tickets!, messages);
                }
                if (fetch)
                {
                    await ReadMails(client, folderRecipients!, settings.DaysBefore, cancellationToken);
                }
            }
        }

        internal static async Task ReadMails(ImapClient client, Dictionary<string, List<string>> folderRecipients, int daysBefore, CancellationToken cancellationToken)
        {
            var tableStorageService = new TableStorageService();
            IMetadataService metadataService = new FabricMetadataService();
            var ticketEntryRepository = new TicketEntryRepository(new AlwaysGetCacheManager<TicketEntity>(metadataService),
                new AlwaysGetCacheManager<AttachmentEntry>(metadataService));

            foreach (var (folderName, recipientsList) in folderRecipients)
            {
                var fromDate = DateTime.Now.AddDays(-daysBefore).ToUniversalTime();
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
                        if (!folder.IsOpen)
                            folder.Open(FolderAccess.ReadOnly, cancellationToken);

                        uids = folder.Search(query, cancellationToken).ToList();

                        List<IMessageSummary> toProcess = new();
                        if (uids?.Any() == true)
                            foreach (var messageSummary in await folder.FetchAsync(uids, MessageSummaryItems.InternalDate
                                | MessageSummaryItems.EmailId
                                | MessageSummaryItems.UniqueId))
                            {
                                if (await ticketEntryRepository.GetTicket(GetPartitionKey(messageSummary), GetRowKey(messageSummary)) != null)
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

        internal static async Task DownloadAll(ImapClient client,
            List<MailSettingEntry> mailSettingEntries,
            IList<TicketEntity> tickets,
            IList<AttachmentEntry> attachmentEntries,
            TableEntityPK[] uids)
        {
            var blob = new BlobAccessStorageService();
            IMetadataService metadataService = new FabricMetadataService();
            var ticketEntryRepository = new TicketEntryRepository(
                new AlwaysGetCacheManager<TicketEntity>(metadataService),
                new AlwaysGetCacheManager<AttachmentEntry>(metadataService)
                );

            List<TableEntityPK> result = new();
            try
            {
                var attachments = attachmentEntries.Where(t => uids.Any(u => u.RowKey == t.RefKey && u.PartitionKey == t.RefPartition)).ToList();
                foreach (var uid in uids)
                {
                    var fname = $"attachments/{uid.PartitionKey}/{uid.RowKey}/body.eml";

                    if (attachments.Any(a => a.RefPartition == uid.PartitionKey && a.RefKey == uid.RowKey))
                    {
                        result.Add(uid);
                        continue;
                    }
                }

                if (result.Count == uids.Count()) return;

                var missingUids = uids.Except(result);
                tickets = [.. tickets.Where(t => missingUids.Any(u => u.PartitionKey == t.PartitionKey && u.RowKey == t.RowKey))];
                var allFolders = mailSettingEntries.SelectMany(t => t.Folders.Split(";", StringSplitOptions.TrimEntries)).Distinct().ToArray();

                var foundIn = tickets.Where(x => !string.IsNullOrEmpty(x.CurrentFolder)).Select(x => x.CurrentFolder)
                                    .Distinct()
                                    .ToList();

                foreach (var folder in GetFolders(client, [.. foundIn.Concat(allFolders.Except(foundIn).ToArray())], CancellationToken.None))
                {
                    var uidsMissing = tickets.Select(t => new UniqueId((uint)t.Validity, (uint)t.Uid)).ToList();
                    if (uidsMissing.Count == 0) return;
                    if (!folder.IsOpen)
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
                        if (!await blob.Exists(fname))
                            using (var fileStream = TempFileHelper.CreateTempFile())
                            {
                                msg.WriteTo(FormatOptions.Default, fileStream);
                                fileStream.Position = 0;
                                await blob.WriteTo(fname, fileStream);
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
                        if (!await blob.Exists(details + "body.html"))
                            await blob.WriteTo(details + "body.html", new BinaryData(visitor.HtmlBody).ToStream());
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
                            if (!await blob.Exists(filePath))
                                using (var fStream = TempFileHelper.CreateTempFile())
                                {
                                    if (attachment is MessagePart)
                                    {
                                        var part = (MessagePart)attachment;
                                        await part.Message.WriteToAsync(fStream);
                                    }
                                    else
                                    {
                                        var part = (MimePart)attachment;
                                        await part.Content.DecodeToAsync(fStream);
                                    }
                                    fStream.Position = 0;
                                    await blob.WriteTo(filePath, fStream);
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
                        result.Add(uu);
                    }
                    tickets = [.. tickets.ExceptBy(found, t => new UniqueId((uint)t.Validity, (uint)t.Uid))];
                }
            }
            catch (Exception ex)
            {
                LogError(ex);
                throw;
            }
        }

        internal static async Task Move(
            ImapClient client,
            List<MailSettingEntry> mailSettingEntries,
            IList<TicketEntity> tickets,
            MoveToMessage<TableEntityPK>[] messages)
        {
            IMetadataService metadataService = new FabricMetadataService();
            var ticketEntryRepository = new TicketEntryRepository(
                new AlwaysGetCacheManager<TicketEntity>(metadataService),
                new AlwaysGetCacheManager<AttachmentEntry>(metadataService)
                );

            var allFolders = mailSettingEntries.SelectMany(t => t.Folders.Split(";", StringSplitOptions.TrimEntries))
                .Distinct()
                .ToArray();

            foreach (var msg in messages)
            {
                var destinationFolder = GetFolders(client, [msg.DestinationFolder], CancellationToken.None).ElementAt(0);

                var entries = tickets.Where(x => msg.Items.Any(f => f.RowKey == x.RowKey && f.PartitionKey == x.PartitionKey)).ToList();
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
                        var mapping = await folder.MoveToAsync(uids, destinationFolder, CancellationToken.None);
                        var moved = entries.Where(x => found.Any(f => f.Validity == x.Validity && f.Id == x.Uid)).ToList();
                        foreach (var x in moved)
                        {
                            var currentKey = new UniqueId((uint)x.Validity, (uint)x.Uid);
                            x.CurrentFolder = msg.DestinationFolder;
                            if (string.IsNullOrEmpty(x.FoundInFolder))
                            {
                                x.FoundInFolder = folder.Name;
                            }
                            if (mapping.ContainsKey(currentKey))
                            {
                                x.Validity = (int)mapping[currentKey].Validity;
                                x.Uid = (int)mapping[currentKey].Id;
                            }
                        }
                        await ticketEntryRepository.Save([.. moved]);

                        uids = [.. uids.Except(found)];
                    }
                }
            }
        }

        private static async Task AddComplaint(IList<IMessageSummary> messages, ITicketEntryRepository ticketEntryRepository, IMailFolder folder)
        {
            BlobAccessStorageService storageService = new();
            IMetadataService metadataService = new FabricMetadataService();
            DataKeyLocationRepository locationRepository = new(new AlwaysGetCacheManager<DataKeyLocationEntry>(metadataService));
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
                string contentId = "";

                string extension = message.HtmlBody != null ? "html" : "txt";
                var fname = $"attachments/{GetPartitionKey(message)}/{GetRowKey(message)}/body.{extension}";

                string body = "";
                try
                {
                    if (await storageService.Exists(fname))
                    {
                        using (var stream = storageService.Access(fname, out var contentType))
                        using (var reader = new StreamReader(stream))
                        {
                            body = reader.ReadToEnd();
                            body = body.Length > 512 ? body.Substring(0, 512) : body;
                        }
                    }
                    else
                    {
                        if (!string.IsNullOrEmpty(message.PreviewText))
                        {
                            using (var stream = File.Open(Guid.NewGuid().ToString(), FileMode.Create))
                            {
                                await storageService.WriteTo(fname, new BinaryData(message.PreviewText).ToStream());
                                body = message.PreviewText;
                            }
                        }
                        else
                        {
                            var bodyPart = message.Folder.GetBodyPart(message.UniqueId, message.Body);
                            var bodyVisitor = new HtmlPreviewVisitor();
                            bodyPart.Accept(bodyVisitor);
                            using (var stream = new MemoryStream())
                            {
                                await storageService.WriteTo(fname, new BinaryData(bodyVisitor.HtmlBody).ToStream());
                                body = bodyVisitor.HtmlBody;
                            }
                            body = body.Trim().Replace("__", "") ?? "";
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
                            TownName = string.Join(";", froms ?? []),
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
                    Locations = string.Join(";", [""]),
                    CreatedDate = message.Date.Date.ToUniversalTime(),
                    NrComanda = "",
                    PartitionKey = GetPartitionKey(message),
                    RowKey = GetRowKey(message),
                    InReplyTo = message.Envelope.InReplyTo,
                    MessageId = message.Envelope.MessageId,
                    References = string.Join(";", message.References),
                    Subject = message.NormalizedSubject,
                    Description = body,
                    ThreadId = message.ThreadId + "_" + message.UniqueId.Validity,
                    EmailId = message.EmailId,
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

            IWorkflowTrigger processQueue = new QueueService();
            await processQueue.Trigger("addmailtotask", toSave.Select(t => new AddMailToTask()
            {
                PartitionKey = t.PartitionKey,
                RowKey = t.RowKey,
                ThreadId = t.ThreadId,
                Date = t.CreatedDate,
                TableName = nameof(TicketEntity),
                LocationRowKey = t?.LocationRowKey ?? "",
                LocationPartitionKey = t?.LocationPartitionKey ?? ""
            }).ToList());
        }

        private static async Task<ImapClient> ConnectAsync(MailSourceEntry settings, CancellationToken cancellationToken)
        {
            ImapClient client = new ImapClient();
            client.ServerCertificateValidationCallback = (s, c, h, e) => true;
            await client.ConnectAsync(Encoding.UTF8.GetString(Convert.FromBase64String(settings.Host)), settings.Port, settings.UseSSL, cancellationToken);
            await client.AuthenticateAsync(Encoding.UTF8.GetString(Convert.FromBase64String(settings.UserName))
                , Encoding.UTF8.GetString(Convert.FromBase64String(settings.Password)), cancellationToken);
            return client;
        }
        private static string GetPartitionKey(IMessageSummary id) => id.UniqueId.Validity.ToString();
        private static string GetRowKey(IMessageSummary id) => id.UniqueId.Id.ToString();
        private static void LogError(Exception ex)
        {
            logger.ActorMessage(actor, "{0}. Stack trace: {1}", ex.Message, ex.StackTrace ?? "");
        }
        private static IEnumerable<IMailFolder> GetFolders(ImapClient client, string[] folders, CancellationToken cancellationToken)
        {
            var personal = client.GetFolder(client.PersonalNamespaces[0]);

            foreach (var folder in folders)
            {
                if (personal.Name.ToLower().Equals(folder.ToLower())) yield return personal;
                else yield return personal.GetSubfolder(folder, cancellationToken);
            }
        }
    }
}
