using AzureTableRepository.CommitedOrders;
using AzureTableRepository.Orders;
using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Actors.Runtime;
using Microsoft.ServiceFabric.Services.Remoting.Client;
using Microsoft.ServiceFabric.Services.Remoting.V2.FabricTransport.Client;
using PollerRecurringJob.Interfaces;
using PollerRecurringJob.JobHandlers;

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
        internal ServiceProxyFactory serviceProxy = new ServiceProxyFactory((c) =>
            {
                return new FabricTransportServiceRemotingClientFactory();
            });

        internal static readonly string SyncOrders = "SyncOrders";
        internal static readonly string MoveTo = "MoveToFolder";

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
            if (reminderName == SyncOrders)
            {
                await OrdersStorageSync.Execute(this);
            }
            else if (reminderName == MoveTo)
            {
                await MoveToFolder.Execute(this);
            }
        }

        public async Task RegisterReminder()
        {
            try
            {
                var previousRegistration = GetReminder(SyncOrders);
                await UnregisterReminderAsync(previousRegistration);
            }
            catch (ReminderNotFoundException) { }
            try
            {
                var previousRegistration = GetReminder(MoveTo);
                await UnregisterReminderAsync(previousRegistration);
            }
            catch (ReminderNotFoundException) { }


            await RegisterReminderAsync(SyncOrders, null, TimeSpan.FromMinutes(0), TimeSpan.FromHours(3));
            await RegisterReminderAsync(MoveTo, null, TimeSpan.FromMinutes(0), TimeSpan.FromMinutes(5));
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
            var commitDate = await commitedOrdersRepository.GetLastSyncDate() ?? new DateTime(2024, 9, 1);
            var oderDate = await ordersRepository.GetLastSyncDate() ?? new DateTime(2024, 5, 5);
            await StateManager.AddOrUpdateStateAsync("commited", commitDate, (x, y) => y);
            await StateManager.AddOrUpdateStateAsync("order", oderDate, (x, y) => y);
        }
    }
}
