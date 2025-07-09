using AzureFabricServices;
using AzureServices;
using AzureTableRepository.DataKeyLocation;
using AzureTableRepository.Tickets;
using EntityDto;
using HtmlAgilityPack;
using MailKit;
using MailKit.Net.Imap;
using MailKit.Search;
using MailKit.Security;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.ServiceFabric.Actors.Runtime;
using MimeKit;
using RepositoryContract;
using RepositoryContract.DataKeyLocation;
using RepositoryContract.MailSettings;
using RepositoryContract.Tickets;
using ServiceImplementation;
using ServiceImplementation.Caching;
using ServiceInterface.Storage;
using System.Runtime.CompilerServices;
using System.Text;
using UniqueId = MailKit.UniqueId;

namespace MailReader.MailOperations
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

        internal static async Task Batch(
            ImapClient client,
            MailSourceEntry settings, List<MailSettingEntry> mSettings,
            MailReader jobContext, Operation op,
            TableEntityPK[] download_uids, MoveToMessage<TableEntityPK>[] messages,
            CancellationToken cancellationToken)
        {
            IMetadataService metadataService = jobContext.provider.GetService<IMetadataService>()!;
            Dictionary<string, List<string>> folderRecipients = null;

            bool downlaod = (op & Operation.Download) == Operation.Download;
            bool move = (op & Operation.Move) == Operation.Move;
            bool fetch = (op & Operation.Fetch) == Operation.Fetch;

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

            if (downlaod)
            {
                await DownloadAll(jobContext, client, mSettings, download_uids);
            }
            if (move)
            {
                await Move(client, mSettings, messages);
            }
            if (fetch)
            {
                await ReadMails(jobContext, client, folderRecipients!, settings.DaysBefore, cancellationToken);
            }
        }

        internal static async Task ReadMails(
            MailReader jobContext, ImapClient client,
            Dictionary<string, List<string>> folderRecipients, int daysBefore,
            CancellationToken cancellationToken)
        {
            TableStorageService tableStorageService = jobContext.provider.GetService<TableStorageService>()!;
            IMetadataService metadataService = jobContext.provider.GetService<IMetadataService>()!;
            ITicketEntryRepository ticketEntryRepository = jobContext.provider.GetService<ITicketEntryRepository>()!;

            foreach (var (folderName, recipientsList) in folderRecipients)
            {
                var fromDate = DateTime.Now.AddDays(-daysBefore).ToUniversalTime();
                var lastRun = tableStorageService.Query<MailEntryStatus>(t => t.PartitionKey == folderName).ToList();
                var startTimer = DateTime.Now.ToUniversalTime();

                foreach (var batchRecipients in recipientsList.Chunk(10))
                {
                    var defaultTimers = batchRecipients.ToDictionary(x => x, v => lastRun.FirstOrDefault(r => r.From == v)?.LastFetch ?? fromDate);
                    var groupedTimers = defaultTimers.GroupBy(x => x.Value).ToDictionary(x => x.Key, v => v.Select(t => t.Key).ToList());
                    IMailFolder? folder = null;

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

                            query = query != null ? query.Or(SearchQuery.DeliveredAfter(item.Key).And(qRecipients)) : SearchQuery.DeliveredAfter(item.Key).And(qRecipients);
                        }

                        List<UniqueId> uids;

                        try
                        {
                            await foreach (var _f in GetFolders(client, [folderName], cancellationToken))
                                folder = _f.folder;

                            if (folder == null)
                            {
                                continue;
                            }
                            if (!folder.IsOpen)
                                await folder.OpenAsync(FolderAccess.ReadOnly, cancellationToken);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(@$"EXAMINE {folderName} - {folder?.Name} failed: " + ex.Message);
                            continue;
                        }

                        uids = [.. await folder.SearchAsync(query, cancellationToken)];

                        List<IMessageSummary> toProcess = new();
                        if (uids?.Any() == true)
                            foreach (var messageSummary in await folder.FetchAsync(uids, MessageSummaryItems.UniqueId
                                        | MessageSummaryItems.InternalDate
                                        | MessageSummaryItems.Envelope
                                        | MessageSummaryItems.EmailId
                                        | MessageSummaryItems.ThreadId))
                            {
                                var partitionKey = GetPartitionKey(messageSummary);
                                var rowKey = GetRowKey(messageSummary);
                                try
                                {
                                    if (await ticketEntryRepository.GetTicket(partitionKey, rowKey) != null
                                        || await ticketEntryRepository.GetTicket(partitionKey, rowKey, $@"{nameof(TicketEntity)}Archive") != null)
                                        continue;

                                    toProcess.Add(messageSummary);
                                }
                                catch (Exception ex)
                                {
                                    LogError(new Exception(@$"{partitionKey} - {rowKey}", ex));
                                }
                            }
                        if (toProcess.Count > 0)
                        {
                            await AddComplaint(jobContext, toProcess, ticketEntryRepository, folder);
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

        internal static async Task DownloadAll(
            MailReader jobContext, ImapClient client,
            List<MailSettingEntry> mailSettingEntries, TableEntityPK[] uids)
        {
            IStorageService blob = jobContext.provider.GetService<IStorageService>()!;
            ITicketEntryRepository ticketEntryRepository = jobContext.provider.GetService<ITicketEntryRepository>()!;
            IList<AttachmentEntry> attachmentEntries = await ticketEntryRepository.GetAllAttachments();
            List<TableEntityPK> result = new();
            try
            {
                var attachments = attachmentEntries.Where(t => uids.Any(u => u.RowKey == t.RefKey && u.PartitionKey == t.RefPartition)).ToList();
                foreach (var uid in uids)
                {
                    if (attachments.Any(a => a.RefPartition == uid.PartitionKey && a.RefKey == uid.RowKey))
                    {
                        result.Add(uid);
                        continue;
                    }
                }

                if (result.Count == uids.Count()) return;

                var missingUids = uids.Except(result);
                List<TicketEntity> tickets = [
                    .. await GetTickets(missingUids, ticketEntryRepository, nameof(TicketEntity)),
                    .. await GetTickets(missingUids, ticketEntryRepository, $@"{nameof(TicketEntity)}Archive")
                    ];

                tickets = [.. tickets.DistinctBy(t => TableEntityPK.From(t.PartitionKey, t.RowKey))];
                IList<UniqueId> found = [];

                foreach (var ticket in tickets.ToList())
                {
                    var fname = $"attachments/{ticket.PartitionKey}/{ticket.RowKey}/body.eml";

                    if (!await blob.Exists(fname))
                    {
                        continue;
                    }
                    await DownloadMessage(null, ticket, ticketEntryRepository, blob);
                    tickets.Remove(ticket);
                }

                if (!tickets.Any()) return;

                var allFolders = mailSettingEntries.SelectMany(t => t.Folders.Split(";", StringSplitOptions.TrimEntries)).Distinct().ToArray();

                var foundIn = tickets.Where(x => !string.IsNullOrEmpty(x.CurrentFolder)).Select(x => x.CurrentFolder)
                                    .Distinct()
                                    .ToList();

                await foreach (var folder2 in GetFolders(client, [.. foundIn.Concat(allFolders.Except(foundIn).ToArray())], CancellationToken.None))
                {
                    var folder = folder2.folder;
                    if (tickets.Count == 0) return;
                    if (!folder.IsOpen)
                        await folder.OpenAsync(FolderAccess.ReadOnly);
                    foreach (var group in tickets.ToList().Chunk(7))
                    {
                        SearchQuery query = SearchQuery.HeaderContains("Message-ID", group[0].MessageId);

                        if (group.Count() > 1)
                        {
                            foreach (var kvp in group.Skip(1))
                            {
                                query = query.Or(SearchQuery.HeaderContains("Message-ID", kvp.MessageId));
                            }
                        }

                        found = await folder.SearchAsync(SearchQuery.Uids(group.Select(x => new UniqueId((uint)x.Validity, (uint)x.Uid)).ToList()).Or(query));
                        var items = await folder.FetchAsync(found, MessageSummaryItems.UniqueId
                                            | MessageSummaryItems.InternalDate
                                            | MessageSummaryItems.Envelope
                                            | MessageSummaryItems.EmailId
                                            | MessageSummaryItems.ThreadId);

                        foreach (var mSummary in items)
                        {
                            var entry = tickets.FirstOrDefault(t => t.PartitionKey == GetPartitionKey(mSummary) && t.RowKey == GetRowKey(mSummary));
                            if (entry != null)
                            {
                                await DownloadMessage(folder.GetMessageAsync(mSummary.UniqueId), entry, ticketEntryRepository, blob);
                                tickets.Remove(entry);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogError(ex);
                throw;
            }
        }

        internal static async Task DownloadMessage(Task<MimeMessage> msgTask, TicketEntity entry, ITicketEntryRepository ticketEntryRepository, IStorageService blob)
        {
            var fname = $"attachments/{entry.PartitionKey}/{entry.RowKey}/body.eml";
            var details = $"attachments/{entry.PartitionKey}/{entry.RowKey}/details/";
            MimeMessage? msg = null;

            if (!await blob.Exists(fname))
                using (var fileStream = TempFileHelper.CreateTempFile())
                {
                    msg = await msgTask;
                    msg.WriteTo(FormatOptions.Default, fileStream);
                    fileStream.Position = 0;
                    await blob.WriteTo(fname, fileStream);
                }
            else
            {
                msg = await MimeMessage.LoadAsync(blob.Access(fname, out _));
            }

            HtmlPreviewVisitor visitor = new();
            msg.Accept(visitor);

            await ticketEntryRepository.Save([new AttachmentEntry()
                                {
                                    PartitionKey = entry.RowKey,
                                    RowKey = entry.Validity + "eml",
                                    Data = fname,
                                    ContentType = "eml",
                                    Title = "body.eml",
                                    RefPartition = entry.PartitionKey,
                                    RefKey = entry.RowKey,
                                }, new AttachmentEntry()
                                {
                                    PartitionKey = entry.RowKey,
                                    RowKey = entry.Validity + "body",
                                    Data = details + "body.html",
                                    Title = "body.html",
                                    ContentType = "html",
                                    RefPartition = entry.PartitionKey,
                                    RefKey = entry.RowKey,
                                }]);

            if (!await blob.Exists(details + "body.html"))
                await blob.WriteTo(details + "body.html", new BinaryData(visitor.HtmlBody).ToStream());

            int idx = 0;
            var attachmentEntries = new List<AttachmentEntry>();
            foreach (var attachment in visitor.Attachments)
            {
                try
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
                    attachmentEntries.Add(new AttachmentEntry()
                    {
                        PartitionKey = entry.RowKey.ToString(),
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
                catch (Exception ex)
                {
                    LogError(ex);
                }
            }

            try
            {
                if (msg.From[0].ToString().Contains("noreply@dedeman.ro"))
                {
                    if (msg.Subject.StartsWith("P.V. rece"))
                    {
                        var link = entry.NrComanda!.Split(";").First(x => x.StartsWith("http"));
                        var refId = entry.NrComanda!.Split(";").First(x => !x.StartsWith("http")) + ".zip";
                        var filePath = details + refId;
                        string contentType = "";
                        if (!await blob.Exists(filePath))
                            using (var fStream = TempFileHelper.CreateTempFile())
                            {
                                contentType = await DownloadFile(link, fStream);
                                await blob.WriteTo(filePath, fStream);
                            }
                        attachmentEntries.Add(new AttachmentEntry()
                        {
                            PartitionKey = entry.RowKey.ToString(),
                            RowKey = Guid.NewGuid().ToString(),
                            Data = filePath,
                            Title = refId,
                            ContentType = contentType,
                            RefPartition = entry.PartitionKey,
                            RefKey = entry.RowKey,
                            ContentId = contentType
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                LogError(ex);
            }

            if (attachmentEntries.Any())
                await ticketEntryRepository.Save([.. attachmentEntries]);
        }

        internal static async Task Move(
            ImapClient client,
            List<MailSettingEntry> mailSettingEntries,
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
                IMailFolder? destinationFolder = null;
                await foreach (var _f in GetFolders(client, [msg.DestinationFolder], CancellationToken.None))
                    destinationFolder = _f.folder;

                if (destinationFolder == null || !destinationFolder.Exists)
                {
                    LogError(new Exception($"Destination folder {msg.DestinationFolder} not found"));
                    continue;
                }

                foreach (var tableName in (string[])[nameof(TicketEntity), $@"{nameof(TicketEntity)}Archive"])
                {
                    var entries = await GetTickets(msg.Items, ticketEntryRepository, tableName);

                    if (!entries.Any()) continue;

                    var foundIn = entries.Where(x => !string.IsNullOrEmpty(x.CurrentFolder))
                                        .Select(x => x.CurrentFolder)
                                        .Distinct()
                                        .ToList();
                    allFolders = [.. foundIn.Concat(allFolders.Except(foundIn))];

                    await foreach (var folder2 in GetFolders(client, allFolders, CancellationToken.None))
                    {
                        try
                        {
                            var folder = folder2.folder;
                            if (folder.Name == destinationFolder.Name) continue;
                            if (!entries.Any()) break;
                            if (folder.CanOpen && !folder.IsOpen)
                                folder.Open(FolderAccess.ReadWrite);

                            foreach (var group in entries.ToList().Chunk(7))
                            {
                                SearchQuery query = SearchQuery.HeaderContains("Message-ID", group[0].MessageId);

                                if (group.Count() > 1)
                                {
                                    foreach (var kvp in group.Skip(1))
                                    {
                                        query = query.Or(SearchQuery.HeaderContains("Message-ID", kvp.MessageId));
                                    }
                                }

                                var uuid = await folder.SearchAsync(SearchQuery.Uids(group.Select(x => new UniqueId((uint)x.Validity, (uint)x.Uid)).ToList()).Or(query));
                                if (uuid.Count == 0) continue;
                                var found = await folder.FetchAsync(uuid, MessageSummaryItems.UniqueId
                                                | MessageSummaryItems.InternalDate
                                                | MessageSummaryItems.Envelope
                                                | MessageSummaryItems.EmailId
                                                | MessageSummaryItems.ThreadId);

                                var toMove = found.Select(f => (f, group.FirstOrDefault(x => GetPartitionKey(f) == x.PartitionKey && GetRowKey(f) == x.RowKey))).Where(t => t.Item2 != null).ToList();
                                var mapping = await folder.MoveToAsync(toMove.Select(x => x.f.UniqueId).ToList(), destinationFolder, CancellationToken.None);

                                foreach (var kvp in toMove)
                                {
                                    var currentKey = kvp.f.UniqueId;
                                    var x = kvp.Item2!;
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
                                    await ticketEntryRepository.Save([x], tableName);
                                    entries.Remove(x);
                                }

                                if (group.Count() != toMove.Count)
                                {
                                    LogError(new Exception(@$"One of the {folder?.Name}::{folder2.folderName} messages wasn't found {string.Join("; ", group.ToList().Except(toMove.Select(t => t.Item2)))}"));
                                }
                            }

                        }
                        catch (Exception ex)
                        {
                            LogError(new Exception(@$"One of the {folder2.folder?.Name}::{folder2.folderName} wasn't moved -- {ex.StackTrace}", ex));
                        }
                    }
                }
            }
        }

        private static async Task AddComplaint(MailReader jobContext, IList<IMessageSummary> messages, ITicketEntryRepository ticketEntryRepository, IMailFolder folder)
        {
            BlobAccessStorageService storageService = new();
            IMetadataService metadataService = new FabricMetadataService();
            DataKeyLocationRepository locationRepository = new(new AlwaysGetCacheManager<DataKeyLocationEntry>(metadataService));

            List<TicketEntity> toSave = [];

            foreach (var _msg in messages)
            {
                string contentId = "";
                var fname = $"attachments/{GetPartitionKey(_msg)}/{GetRowKey(_msg)}/body.eml";

                string description = "";
                string body = "";
                MimeMessage message = await storageService.Exists(fname) ? await MimeMessage.LoadAsync(storageService.Access(fname, out var _)) : await _msg.Folder.GetMessageAsync(_msg.UniqueId);
                if (!await storageService.Exists(fname))
                    using (var fileStream = TempFileHelper.CreateTempFile())
                    {
                        message.WriteTo(FormatOptions.Default, fileStream);
                        fileStream.Position = 0;
                        await storageService.WriteTo(fname, fileStream);
                    }
                try
                {
                    body = message.HtmlBody.Trim().Replace("__", "") ?? "";
                    description = body.Length > 2048 ? body.Substring(0, 2048) : body;
                }
                catch (Exception ex)
                {
                    LogError(ex);
                    description = "Error";
                }

                string[] froms = [];
                string[] NumarComanda = [];
                if (message.From.Any())
                {
                    if (message.From[0].ToString().Contains("noreply@dedeman.ro"))
                    {
                        if (message.Subject.StartsWith("P.V. rece"))
                        {
                            var doc = new HtmlDocument();
                            doc.LoadHtml(message.HtmlBody);
                            froms = [Extract(doc, $@"//td[starts-with(normalize-space(), 'Loc')]")];
                            NumarComanda = [Extract(doc, $@"//td[starts-with(normalize-space(), 'Recep')]"), Extract(doc, $@"//td[starts-with(normalize-space(), 'Link')]")];
                        }
                    }
                    else
                    {
                        froms = message.From.Select(t => t.ToString()).ToArray() ?? [];
                    }
                }

                DataKeyLocationEntry? location = null;
                DataKeyLocationEntry? locationMain = null;
                var locations = await locationRepository.GetLocations();

                if (froms.Any())
                {
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
                    Sender = message.Sender?.ToString(),
                    From = string.Join(";", froms!),
                    Locations = string.Join(";", [""]),
                    CreatedDate = message.Date.Date.ToUniversalTime(),
                    NrComanda = string.Join(";", NumarComanda!),
                    PartitionKey = GetPartitionKey(_msg),
                    RowKey = GetRowKey(_msg),
                    InReplyTo = message.InReplyTo,
                    MessageId = message.MessageId,
                    References = string.Join(";", message.References ?? []),
                    Subject = message.Subject,
                    Description = description,
                    ThreadId = _msg.ThreadId + "_" + _msg.UniqueId.Validity,
                    EmailId = _msg.EmailId,
                    Uid = Convert.ToInt32(_msg.UniqueId.Id),
                    Validity = Convert.ToInt32(_msg.UniqueId.Validity),
                    OriginalBodyPath = fname,
                    LocationCode = location?.LocationCode,
                    LocationRowKey = location?.RowKey,
                    LocationPartitionKey = location?.PartitionKey,
                    HasAttachments = message.Attachments?.Any() == true,
                    FoundInFolder = _msg.Folder.Name,
                    CurrentFolder = _msg.Folder.Name
                });
            }
            await ticketEntryRepository.Save([.. toSave]);
            await AddNewMailToExistingTasks.Execute(jobContext, toSave.Select(t => new AddMailToTask()
            {
                PartitionKey = t.PartitionKey,
                RowKey = t.RowKey,
                ThreadId = t.ThreadId,
                Date = t.CreatedDate,
                TableName = nameof(TicketEntity),
                EntityType = nameof(TicketEntity),
                LocationRowKey = t?.LocationRowKey ?? "",
                LocationPartitionKey = t?.LocationPartitionKey ?? ""
            }).ToList());
        }

        static string Extract(HtmlDocument doc, string select)
        {
            // Modify the XPath to suit your specific email structure
            var linkNode = doc.DocumentNode.SelectSingleNode(select);

            if (linkNode != null)
            {
                // Get the next sibling <td>
                var valueTd = linkNode.SelectSingleNode("following-sibling::td[1]");
                return valueTd?.InnerText.Trim();
            }
            Console.WriteLine("Label not found.");
            return null;
        }

        internal static async Task<ImapClient> ConnectAsync(MailSourceEntry settings, CancellationToken cancellationToken)
        {
            try
            {
                ImapClient client = new ImapClient();
                client.ServerCertificateValidationCallback = (s, c, h, e) => true;

                for (int attempt = 1; attempt <= 3; attempt++)
                {
                    try
                    {
                        await client.ConnectAsync(Encoding.UTF8.GetString(Convert.FromBase64String(settings.Host)), settings.Port, SecureSocketOptions.SslOnConnect, cancellationToken);
                        break; // Success
                    }
                    catch (SslHandshakeException ex) when (attempt < 3)
                    {
                        LogError(ex);
                        await Task.Delay(1000 * attempt); // Wait longer each attempt
                    }
                }

                await client.AuthenticateAsync(Encoding.UTF8.GetString(Convert.FromBase64String(settings.UserName))
                    , Encoding.UTF8.GetString(Convert.FromBase64String(settings.Password)), cancellationToken);
                return client;
            }
            catch (Exception ex)
            {
                LogError(ex);
                return null;
            }
        }

        private static string GetPartitionKey(IMessageSummary id)
        {
            return AzureTableKeySanitizer.Sanitize(id.Envelope.Date.GetValueOrDefault(id.Date).ToString("yyMM"));
        }

        private static string GetRowKey(IMessageSummary id)
        {
            var part1 = Math.Abs(GetStableHashCode(id.Envelope.Subject + id.Envelope.Date?.ToString() + string.Join(";", id.Envelope.From ?? []))).ToString();
            var rowKey = !string.IsNullOrEmpty(id.Envelope.MessageId) ? id.Envelope.MessageId : part1;

            return AzureTableKeySanitizer.Sanitize($@"{rowKey}_{Math.Abs(GetStableHashCode(part1 + id.Date.ToString()))}");
        }
        private static void LogError(Exception ex)
        {
            logger.ActorMessage(actor, "{0}. Stack trace: {1}", ex.Message, ex.StackTrace ?? "");
        }
        private static async IAsyncEnumerable<(IMailFolder folder, string folderName)> GetFolders(ImapClient client, string[] folders, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            await Task.Yield();

            var personal = client.GetFolder(client.PersonalNamespaces[0]);
            foreach (var folder in folders)
            {
                IMailFolder? result = null;
                try
                {
                    if (personal.Name.ToLower().Equals(folder.ToLower()))
                    {
                        result = personal;
                    }
                    else
                    {
                        result = await personal.GetSubfolderAsync(folder, cancellationToken);
                    }
                }
                catch (Exception ex)
                {
                    LogError(new Exception($"Failed to get folder {folder}", ex));
                }

                if (result?.Exists == true)
                {
                    yield return (result, folder);
                }
                else
                {
                    LogError(new Exception($"Folder {folder} does not exist or could not be accessed."));
                }
            }
        }

        private static async Task<string> DownloadFile(string url, Stream fStream)
        {
            string destinationPath = @"file.zip";

            using HttpClient client = new HttpClient();

            try
            {
                using HttpResponseMessage response = await client.GetAsync(url);
                response.EnsureSuccessStatusCode(); // Throws if not 2xx
                using Stream contentStream = await response.Content.ReadAsStreamAsync();
                await contentStream.CopyToAsync(fStream);
                fStream.Position = 0;
                Console.WriteLine("Download completed: " + destinationPath);
                return response.Content.Headers.ContentType.MediaType;
            }
            catch (Exception ex)
            {
                LogError(ex);
            }

            return "";
        }

        private static int GetStableHashCode(string? str)
        {
            if (str == null) return 0;
            unchecked
            {
                int hash1 = 5381;
                int hash2 = hash1;

                for (int i = 0; i < str.Length && str[i] != '\0'; i += 2)
                {
                    hash1 = ((hash1 << 5) + hash1) ^ str[i];
                    if (i == str.Length - 1 || str[i + 1] == '\0')
                        break;
                    hash2 = ((hash2 << 5) + hash2) ^ str[i + 1];
                }

                return hash1 + (hash2 * 1566083941);
            }
        }

        private static async Task<List<TicketEntity>> GetTickets(IEnumerable<TableEntityPK> tableEntityPKs, ITicketEntryRepository repository, string tableName)
        {
            List<TicketEntity> ticketEntities = new();
            foreach (var tableEntity in tableEntityPKs.GroupBy(t => t.PartitionKey))
            {
                ticketEntities.AddRange(await repository.GetSome(tableName, tableEntity.Key, tableEntity.Min(t => t.RowKey), tableEntity.Max(t => t.RowKey)));
            }

            return [.. ticketEntities.IntersectBy(tableEntityPKs, t => TableEntityPK.From(t.PartitionKey, t.RowKey), TableEntityPK.GetComparer<TableEntityPK>())];
        }
    }
}
