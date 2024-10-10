using AzureServices;
using AzureTableRepository.CommitedOrders;
using AzureTableRepository.Orders;
using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Actors.Runtime;
using PollerRecurringJob.Interfaces;
using SqlTableRepository.Orders;

namespace PollerRecurringJob
{
    /// <remarks>
    /// This class represents an actor.
    /// Every ActorID maps to an instance of this class.
    /// The StatePersistence attribute determines persistence and replication of actor state:
    ///  - Persisted: State is written to disk and replicated.
    ///  - Volatile: State is kept in memory only and replicated.
    ///  - None: State is kept in memory only and not replicated.
    /// </remarks>
    [StatePersistence(StatePersistence.Persisted)]
    internal class PollerRecurringJob : Actor, IPollerRecurringJob, IActor1, IRemindable
    {
        /// <summary>
        /// Initializes a new instance of PollerRecurringJob
        /// </summary>
        /// <param name="actorService">The Microsoft.ServiceFabric.Actors.Runtime.ActorService that will host this actor instance.</param>
        /// <param name="actorId">The Microsoft.ServiceFabric.Actors.ActorId for this actor instance.</param>
        public PollerRecurringJob(ActorService actorService, ActorId actorId)
            : base(actorService, actorId)
        {
        }

        public async Task ReceiveReminderAsync(string reminderName, byte[] state, TimeSpan dueTime, TimeSpan period)
        {
            var storage = new BlobAccessStorageService();
            var ordersImportsRepository = new OrdersImportsRepository(storage);
            var commitedOrdersRepository = new CommitedOrdersRepository(null);
            var ordersRepository = new OrdersRepository(null);

            var lastOrder = await StateManager.GetOrAddStateAsync("order", DateTime.Now.AddDays(-7));
            var lastCommited = await StateManager.GetOrAddStateAsync("commited", DateTime.Now.AddDays(-7));

            var latest = await ordersImportsRepository.PollForNewContent();

            if (latest.order > lastOrder && latest.commited > lastCommited)
            {
                var sourceOrders = await ordersImportsRepository.GetImportCommitedOrders(lastCommited, lastOrder);
                await commitedOrdersRepository.ImportCommitedOrders(sourceOrders.commited, latest.commited);
                await ordersRepository.ImportOrders(sourceOrders.orders, latest.order);
            }
            else if (latest.order > lastOrder)
            {
                var sourceOrders = await ordersImportsRepository.GetImportOrders(lastOrder);
                await ordersRepository.ImportOrders(sourceOrders, latest.order);
            }
            else if (latest.commited > lastCommited)
            {
                var commited = await ordersImportsRepository.GetImportCommited(lastCommited);
                await commitedOrdersRepository.ImportCommitedOrders(commited, latest.commited);
            }
            await StateManager.AddOrUpdateStateAsync("order", latest.order, (_, _) => latest.order);
            await StateManager.AddOrUpdateStateAsync("commited", latest.commited, (_, _) => latest.commited);
        }

        public async Task RegisterReminder()
        {
            try
            {
                var previousRegistration = GetReminder("Reminder1");
                await UnregisterReminderAsync(previousRegistration);
            }
            catch (ReminderNotFoundException) { }

            var reminderRegistration = await RegisterReminderAsync("Reminder1", null, TimeSpan.FromMinutes(0), TimeSpan.FromMinutes(1));
        }

        public async Task Sync()
        {
            await RegisterReminder();
        }

        /// <summary>
        /// This method is called whenever an actor is activated.
        /// An actor is activated the first time any of its methods are invoked.
        /// </summary>
        protected override async Task OnActivateAsync()
        {
            ActorEventSource.Current.ActorMessage(this, "Polling Actor activated.");
            var commitedOrdersRepository = new CommitedOrdersRepository(null);
            var ordersRepository = new OrdersRepository(null);
            var commitDate = await commitedOrdersRepository.GetLastSyncDate();
            var oderDate = await ordersRepository.GetLastSyncDate();
            await StateManager.AddStateAsync("commited", commitDate);
            await StateManager.AddStateAsync("order", oderDate);

            await RegisterReminder();
        }
    }
}
