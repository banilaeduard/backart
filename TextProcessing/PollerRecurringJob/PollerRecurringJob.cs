using Microsoft.Extensions.DependencyInjection;
using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Actors.Runtime;
using Microsoft.ServiceFabric.Services.Remoting.Client;
using Microsoft.ServiceFabric.Services.Remoting.V2.FabricTransport.Client;
using PollerRecurringJob.Interfaces;
using PollerRecurringJob.JobHandlers;
using RepositoryContract.CommitedOrders;
using RepositoryContract.Orders;

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
    [StatePersistence(StatePersistence.None)]
    internal class PollerRecurringJob : Actor, IPollerRecurringJob, IRemindable
    {
        internal ServiceProxyFactory serviceProxy = new ServiceProxyFactory((c) =>
            {
                return new FabricTransportServiceRemotingClientFactory();
            });

        internal static readonly string SyncOrders = "SyncOrders";
        internal static readonly TimeSpan SyncOrdersDue = TimeSpan.FromMinutes(5);
        internal static readonly string MoveTo = "MoveToFolder";
        internal static readonly TimeSpan MoveToDue = TimeSpan.FromMinutes(7);
        internal static readonly string AddNewMail = "AddNewMailToExistingTasks";
        internal static readonly TimeSpan AddNewMailDue = TimeSpan.FromMinutes(13);
        internal static readonly string SyncMails = "SyncNewMails";
        internal static readonly TimeSpan SyncMailsDue = TimeSpan.FromMinutes(17);
        internal static readonly string Remove0ExternalRefs = "Remove0ExternalRefs";
        internal static readonly TimeSpan Remove0ExternalRefsDue = TimeSpan.FromMinutes(2);
        internal static readonly string RemoveLostAttachmentsRefs = "RemoveLostAttachments";
        internal static readonly TimeSpan RemoveLostAttachmentsRefsDue = TimeSpan.FromMinutes(3);
        internal static readonly string TransportJob = "TransportJob";
        internal static readonly TimeSpan TransportJobDue = TimeSpan.FromMinutes(3);
        internal readonly ServiceProvider provider;

        /// <summary>
        /// Initializes a new instance of PollerRecurringJob
        /// </summary>
        /// <param name="actorService">The Microsoft.ServiceFabric.Actors.Runtime.ActorService that will host this actor instance.</param>
        /// <param name="actorId">The Microsoft.ServiceFabric.Actors.ActorId for this actor instance.</param>
        public PollerRecurringJob(ActorService actorService, ActorId actorId, ServiceProvider provider)
            : base(actorService, actorId)
        {
            this.provider = provider;
        }

        public async Task ReceiveReminderAsync(string reminderName, byte[] state, TimeSpan dueTime, TimeSpan period)
        {
            try
            {
                if (reminderName == SyncOrders)
                {
                    await OrdersStorageSync.Execute(this);
                }
                else if (reminderName == MoveTo)
                {
                    await MoveToFolder.Execute(this);
                }
                else if (reminderName == AddNewMail)
                {
                    await AddNewMailToExistingTasks.Execute(this);
                }
                else if (reminderName == SyncMails)
                {
                    await SyncMailsExec.Execute(this);
                }
                else if (reminderName == Remove0ExternalRefs)
                {
                    await Remove0ExternalRefsSync.Execute(this);
                }
                else if (reminderName == RemoveLostAttachmentsRefs)
                {
                    await RemoveLostAttachments.Execute(this);
                }
                else if (reminderName == TransportJob)
                {
                    await TransportAttachment.Execute(this);
                }
            }
            catch (Exception ex)
            {
                ActorEventSource.Current.ActorMessage(this, @$"EXCEPTION POLLER: {ex.Message}. {ex.StackTrace ?? ""}");
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
            try
            {
                var previousRegistration = GetReminder(AddNewMail);
                await UnregisterReminderAsync(previousRegistration);
            }
            catch (ReminderNotFoundException) { }
            try
            {
                var previousRegistration = GetReminder(SyncMails);
                await UnregisterReminderAsync(previousRegistration);
            }
            catch (ReminderNotFoundException) { }
            try
            {
                var previousRegistration = GetReminder(Remove0ExternalRefs);
                await UnregisterReminderAsync(previousRegistration);
            }
            catch (ReminderNotFoundException) { }
            try
            {
                var previousRegistration = GetReminder(RemoveLostAttachmentsRefs);
                await UnregisterReminderAsync(previousRegistration);
            }
            catch (ReminderNotFoundException) { }
            try
            {
                var previousRegistration = GetReminder(TransportJob);
                await UnregisterReminderAsync(previousRegistration);
            }
            catch (ReminderNotFoundException) { }


            await RegisterReminderAsync(SyncOrders, null, TimeSpan.FromMinutes(3), SyncOrdersDue);
            await RegisterReminderAsync(MoveTo, null, TimeSpan.FromMinutes(15), MoveToDue);
            await RegisterReminderAsync(AddNewMail, null, TimeSpan.FromMinutes(15), AddNewMailDue);
            await RegisterReminderAsync(SyncMails, null, TimeSpan.FromMinutes(30), SyncMailsDue);
            await RegisterReminderAsync(Remove0ExternalRefs, null, TimeSpan.FromMinutes(0), Remove0ExternalRefsDue);
            await RegisterReminderAsync(RemoveLostAttachmentsRefs, null, TimeSpan.FromMinutes(0), RemoveLostAttachmentsRefsDue);
            await RegisterReminderAsync(TransportJob, null, TimeSpan.FromMinutes(0), TransportJobDue);
        }

        public async Task SyncOrdersAndCommited()
        {
            await OrdersStorageSync.Execute(this);
            _ = Task.Run(async () => await RegisterReminder());
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
            var commitedOrdersRepository = provider.GetService<ICommitedOrdersRepository>()!;
            var ordersRepository = provider.GetService<IOrdersRepository>()!;
            var commitDate = await commitedOrdersRepository.GetLastSyncDate() ?? new DateTime(2024, 9, 1);
            var oderDate = await ordersRepository.GetLastSyncDate() ?? new DateTime(2024, 5, 5);
        }
    }
}
