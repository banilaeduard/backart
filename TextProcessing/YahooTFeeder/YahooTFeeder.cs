using System.Fabric;
using System.IO;
using System.Text.RegularExpressions;
using AzureServices;
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
using Services.Storage;
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
            await Task.Delay(TimeSpan.FromHours(3));
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
            var ticketEntryRepository = new TicketEntryRepository(null);

            using (ImapClient client = new ImapClient())
            {
                await client.ConnectAsync(
                    "imap.mail.yahoo.com",
                    993,
                    true, cancellationToken); //For SSL
                await client.AuthenticateAsync(settings.User, settings.Password, cancellationToken);

                foreach (var folder in GetFolders(client, settings.Folders, cancellationToken))
                {
                    foreach (var from in settings.From)
                    {
                        try
                        {
                            IList<UniqueId> uids;
                            DateTime fromDate = DateTime.Now.AddDays(-settings.DaysBefore);

                            folder.Open(FolderAccess.ReadOnly, cancellationToken);

                            uids = folder.Search(
                                SearchQuery.DeliveredAfter(fromDate).And(
                                  SearchQuery.FromContains(from)
                                )
                            , cancellationToken);

                            foreach (var uid in uids)
                            {
                                try
                                {
                                    var message = folder.GetMessage(uid, cancellationToken);

                                    var body = await getBody(message);

                                    var extras = await serviceProxy.CreateServiceProxy<IMailExtrasExtractor>(new Uri("fabric:/TextProcessing/MailExtrasExtractorType")).Parse(body);

                                    if (await ticketEntryRepository.Exists<TicketEntity>(message.Date.Date.ToString("MMyy"), uid.ToString()))
                                        continue;

                                    var ticket = await AddComplaint(message, extras, from, uid, ticketEntryRepository);
                                    await SaveAttachments(message, ticket, ticketEntryRepository);
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
                        }
                    }
                }
                client.Disconnect(true, cancellationToken);
            }
        }

        private async Task<TicketEntity> AddComplaint(MimeMessage message, Extras extras, string from, UniqueId uid, ITicketEntryRepository ticketEntryRepository)
        {
            BlobAccessStorageService storageService = new();
            var body = !string.IsNullOrWhiteSpace(message.HtmlBody) ? regexHtml.Replace(message.HtmlBody, " ") : message.TextBody;

            var ticket = new TicketEntity()
            {
                From = extras.Addreses.Length > 0 ? string.Join(";", extras.Addreses) : message.From.FirstOrDefault()!.Name,
                CreatedDate = message.Date.Date.ToUniversalTime(),
                NrComanda = extras.NumarComanda ?? "",
                TicketSource = "Mail",
                PartitionKey = message.Date.Date.ToString("MMyy"),
                RowKey = uid.ToString(),
                InReplyTo = message.InReplyTo,
                MessageId = message.MessageId,
                ResentReplyTo = string.Join(";", message.ResentReplyTo.Select(t => t.Name)),
                From2 = string.Join(";", message.From.Select(t => t.Name)),
                ResentFrom = string.Join(";", message.ResentFrom.Select(t => t.Name)),
            };

            var extension = string.IsNullOrWhiteSpace(message.HtmlBody) ? "txt" : "html";
            var fname = $"attachments/{DateTime.Now.ToString("MMyy")}/{ticket.RowKey}_body.{extension}";
            storageService.WriteTo(fname, new BinaryData(System.Text.Encoding.UTF8.GetBytes(body)));
            ticket.Description = fname;

            await ticketEntryRepository.Save(ticket);
            return ticket;
        }

        private async Task SaveAttachments(MimeMessage message, TicketEntity ticket, ITicketEntryRepository ticketEntryRepository)
        {
            BlobAccessStorageService storageService = new();

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

                        MimeTypes.TryGetExtension(attachment.ContentType.MimeType, out var extension);
                        var fname = $"attachments/{DateTime.Now.ToString("MMyy")}/{ticket.RowKey}_{Guid.NewGuid()}.{extension ?? "txt"}";

                        storageService.WriteTo(fname, new BinaryData(stream.ToArray()));

                        await ticketEntryRepository.Save(new AttachmentEntry()
                        {
                            PartitionKey = ticket.RowKey,
                            RowKey = Guid.NewGuid().ToString(),
                            Data = fname,
                            ContentType = attachment.ContentType.MimeType,
                            RefPartition = ticket.PartitionKey,
                            RefKey = ticket.RowKey,
                        });
                    }
                }
            }
        }
    }
}
