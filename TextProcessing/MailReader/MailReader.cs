using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Actors.Runtime;
using MailReader.Interfaces;
using MailReader.MailOperations;
using EntityDto;
using RepositoryContract;
using Microsoft.Extensions.DependencyInjection;
using ServiceInterface.Storage;
using MailKit.Net.Imap;
using RepositoryContract.MailSettings;

namespace MailReader
{
    /// <remarks>
    /// This class represents an actor.
    /// Every ActorID maps to an instance of this class.
    /// The StatePersistence attribute determines persistence and replication of actor state:
    ///  - Persisted: State is written to disk and replicated.
    ///  - Volatile: State is kept in memory only and replicated.
    ///  - None: State is kept in memory only and not replicated.
    /// </remarks>
    [StatePersistence(StatePersistence.None)]
    internal class MailReader : Actor, IMailReader
    {
        internal static string Source;
        internal readonly ServiceProvider provider;
        /// <summary>
        /// Initializes a new instance of MailReader
        /// </summary>
        /// <param name="actorService">The Microsoft.ServiceFabric.Actors.Runtime.ActorService that will host this actor instance.</param>
        /// <param name="actorId">The Microsoft.ServiceFabric.Actors.ActorId for this actor instance.</param>
        public MailReader(ActorService actorService, ActorId actorId, ServiceProvider provider)
            : base(actorService, actorId)
        {
            this.provider = provider;
        }

        internal async Task<(MailSourceEntry mailSource, List<MailSettingEntry> mailSettingEntries)> GetSettings()
        {
            IMailSettingsRepository mailSettings = provider.GetService<IMailSettingsRepository>()!;

            var settings = (await mailSettings.GetMailSource()).FirstOrDefault(t => t.PartitionKey == Source);

            if (settings == null)
            {
                ActorEventSource.Current.ActorMessage(this, "No settings for {0}. Cannot run the mail service", Source);
                throw new ArgumentException("MAIL SOURCE");
            }
            var mSettings = (await mailSettings.GetMailSetting(settings.PartitionKey)).ToList();

            return (settings, mSettings);
        }

        public async Task BatchAsync(List<MoveToMessage<TableEntityPK>> move)
        {
            var cfg = await GetSettings();
            var downloadLazy = move.Where(x => x.DestinationFolder == "_PENDING_").SelectMany(x => x.Items).Distinct().ToList();

            using (ImapClient client = await YahooTFeeder.ConnectAsync(cfg.mailSource, CancellationToken.None))
            {
                await YahooTFeeder.Batch(client, cfg.mailSource, cfg.mailSettingEntries,
                    this,
                    Operation.Download | Operation.Move | Operation.Fetch,
                    [.. downloadLazy],
                    [.. move],
                CancellationToken.None);

                await GetMoveQueue(async (moveNew, downloadNew) =>
                {
                    await YahooTFeeder.Batch(client, cfg.mailSource, cfg.mailSettingEntries,
                    this,
                    Operation.Move | Operation.Download,
                    [.. downloadNew],
                    [.. moveNew],
                CancellationToken.None);
                });
            }
        }

        public async Task DownloadAll(List<TableEntityPK> entityPKs)
        {
            var cfg = await GetSettings();
            using (ImapClient client = await YahooTFeeder.ConnectAsync(cfg.mailSource, CancellationToken.None))
            {
                await YahooTFeeder.Batch(client, cfg.mailSource, cfg.mailSettingEntries,
                this,
                Operation.Download, [.. entityPKs], [], CancellationToken.None);
            }
        }

        public async Task FetchMails()
        {
            var cfg = await GetSettings();

            using (ImapClient client = await YahooTFeeder.ConnectAsync(cfg.mailSource, CancellationToken.None))
            {
                await GetMoveQueue(async (moveNew, downloadNew) =>
                {
                    await YahooTFeeder.Batch(client, cfg.mailSource, cfg.mailSettingEntries,
                    this,
                    Operation.Move | Operation.Fetch | Operation.Download,
                    [.. downloadNew],
                    [.. moveNew],
                CancellationToken.None);
                });

                await GetMoveQueue(async (moveNew, downloadNew) =>
                {
                    await YahooTFeeder.Batch(client, cfg.mailSource, cfg.mailSettingEntries,
                    this,
                    Operation.Move | Operation.Download,
                    [.. downloadNew],
                    [.. moveNew],
                CancellationToken.None);
                });
            }
        }

        internal async Task GetMoveQueue(Action<List<MoveToMessage<TableEntityPK>>, List<TableEntityPK>> action)
        {
            IWorkflowTrigger client = provider.GetRequiredService<IWorkflowTrigger>()!;
            var items = await client.GetWork<MoveToMessage<TableEntityPK>>("movemailto");

            var finalList = new Dictionary<TableEntityPK, string>(TableEntityPK.GetComparer<TableEntityPK>());
            foreach (var message in items.OrderBy(t => t.Timestamp))
            {
                foreach (var item in message.Model.Items)
                    finalList[item] = message.Model.DestinationFolder;
            }

            List<MoveToMessage<TableEntityPK>> move = new();
            if (finalList.Any())
            {
                move = finalList.GroupBy(l => l.Value).Select(x =>
                    new MoveToMessage<TableEntityPK>()
                    {
                        DestinationFolder = x.Key,
                        Items = x.Select(it => it.Key).Distinct()
                    }).ToList();
            }
            var downloadLazy = move.Where(x => x.DestinationFolder == "_PENDING_").SelectMany(x => x.Items).Distinct().ToList();

            if (move.Any() || downloadLazy.Any())
            {
                action(move, downloadLazy);
                await client.ClearWork("movemailto", [.. items]);
            }
        }

        /// <summary>
        /// This method is called whenever an actor is activated.
        /// An actor is activated the first time any of its methods are invoked.
        /// </summary>
        protected async override Task OnActivateAsync()
        {
            YahooTFeeder.logger = ActorEventSource.Current;
            YahooTFeeder.actor = this;
#if DEBUG
            return;
#else
            Source = this.GetActorId().GetStringId();
#endif
        }
    }
}
