using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Actors.Runtime;
using MailReader.Interfaces;
using MailReader.MailOperations;
using EntityDto;
using RepositoryContract;
using Microsoft.Extensions.DependencyInjection;

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

        public async Task BatchAsync(List<MoveToMessage<TableEntityPK>> move)
        {
            var downloadLazy = move.Where(x => x.DestinationFolder == "_PENDING_").SelectMany(x => x.Items).Distinct().ToList();
            await YahooTFeeder.Batch(this,
                Source,
                Operation.Download | Operation.Move | Operation.Fetch,
                [.. downloadLazy],
                [.. move],
                CancellationToken.None);
        }

        public async Task DownloadAll(List<TableEntityPK> entityPKs)
        {
            await YahooTFeeder.Batch(this,
                Source,
                Operation.Download, [.. entityPKs], null, CancellationToken.None);
        }

        public async Task FetchMails()
        {
            await YahooTFeeder.Batch(this,
                Source,
                Operation.Fetch, null, null, CancellationToken.None);
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
