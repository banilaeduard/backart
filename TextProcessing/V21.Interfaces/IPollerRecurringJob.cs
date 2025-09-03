using Microsoft.ServiceFabric.Actors;

namespace PollerRecurringJob.Interfaces
{
    /// <summary>
    /// This interface defines the methods exposed by an actor.
    /// Clients use this interface to interact with the actor that implements it.
    /// </summary>
    public interface IPollerRecurringJob : IActor
    {
        Task Sync();
        Task ArchiveMail();
        Task<string> SyncOrdersAndCommited();
    }
}
