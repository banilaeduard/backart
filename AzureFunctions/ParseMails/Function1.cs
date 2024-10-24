using Azure.Storage.Queues.Models;
using AzureServices;
using MailKit;
using MailKit.Net.Imap;
using MailKit.Search;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

namespace ParseMails
{
    public class Function1
    {
        private readonly string blobPrefix = $"pending";
        private readonly ILogger<Function1> _logger;
        private readonly BlobAccessStorageService blobStorage;

        public Function1(ILogger<Function1> logger, ILogger<TableStorageService> logger_storage)
        {
            _logger = logger;
            blobStorage = new();
        }

        [Function(nameof(Function1))]
        public void Run([QueueTrigger("items")] QueueMessage message)
        {
            JObject jObject = JObject.Parse(message.Body.ToString());

            DateTime from = DateTime.Parse((string)jObject["from"]!);
            DateTime? to = null;

            if (jObject.ContainsKey("to"))
            {
                to = DateTime.Parse((string)jObject["to"]!);
            }

            string[] folders = [.. jObject["folders"]!.ToArray().Select(t => t.Value<string>())];
            string[] recipients = [.. jObject["recipients"]!.ToArray().Select(t => t.Value<string>())];

            ReadMails(from, to, folders, recipients, CancellationToken.None);
            _logger.LogInformation($"C# Queue trigger function processed: {message.MessageText}");
        }

        void ReadMails(DateTime from, DateTime? to, string[] folders, string[] recipients, CancellationToken cancellationToken)
        {
            using (ImapClient client = Connect())
            {
                foreach (var folder in GetFolders(client, folders, cancellationToken))
                {
                    foreach (var recipient in recipients)
                    {
                        List<UniqueId> uids;
                        try
                        {
                            SearchQuery query = SearchQuery.FromContains(recipient).Or(SearchQuery.CcContains(recipient)).Or(SearchQuery.ToContains(recipient));

                            SearchQuery searchQuery = SearchQuery.DeliveredAfter(from);
                            if (to.HasValue)
                                searchQuery = searchQuery.And(SearchQuery.DeliveredBefore(to.Value));

                            folder.Open(FolderAccess.ReadOnly, cancellationToken);
                            uids = folder.Search(
                                searchQuery.And(query)
                            , cancellationToken).ToList();

                            foreach (var u in uids)
                            {
                                try
                                {
                                    blobStorage.SetMetadata($"{blobPrefix}/{u.Validity}_{u.Id}", null, new Dictionary<string, string>
                                    {
                                        { "from", recipient },
                                        { "folders", string.Join(';', folders) },
                                        { "foundinfolder", folder.Name },
                                        { "id", u.Id.ToString() },
                                        { "validity", u.Validity.ToString() },
                                    });
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogError(500, ex.StackTrace ?? ex.Message);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(500, ex.StackTrace ?? ex.Message);
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

        IEnumerable<IMailFolder> GetFolders(ImapClient client, string[] folders, CancellationToken cancellationToken)
        {
            var personal = client.GetFolder(client.PersonalNamespaces[0]);

            foreach (var folder in folders)
            {
                yield return personal.GetSubfolder(folder, cancellationToken);
            }
        }

        private static ImapClient Connect()
        {
            ImapClient client = new ImapClient();
            client.Connect(Environment.GetEnvironmentVariable("imap"), 993, true);
            client.Authenticate(Environment.GetEnvironmentVariable("MUser"), Environment.GetEnvironmentVariable("MUPassword"));
            return client;
        }
    }
}
