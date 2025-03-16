using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Actors.Runtime;
using V2_1.Interfaces;

namespace MetadataActor
{
    [StatePersistence(StatePersistence.Volatile)]
    internal class MetadataActor : Actor, IMetadataActor
    {
        /// <summary>
        /// Initializes a new instance of MetadataActor
        /// </summary>
        /// <param name="actorService">The Microsoft.ServiceFabric.Actors.Runtime.ActorService that will host this actor instance.</param>
        /// <param name="actorId">The Microsoft.ServiceFabric.Actors.ActorId for this actor instance.</param>
        public MetadataActor(ActorService actorService, ActorId actorId)
            : base(actorService, actorId)
        {
        }

        public async Task<IDictionary<string, string>> GetMetadata(string fName)
        {
            if (await StateManager.ContainsStateAsync(fName))
            {
                return await StateManager.GetStateAsync<IDictionary<string, string>>(fName);
            }

            return new Dictionary<string, string>();
        }

        public async Task SetMetadata(string fName, IDictionary<string, string> metadata)
        {
            if (metadata != null)
            {
                await StateManager.SetStateAsync(fName, metadata);
            }
            else
            {
                if (await StateManager.ContainsStateAsync(fName))
                {
                    await StateManager.RemoveStateAsync(fName);
                }
            }
        }

        /// <summary>
        /// This method is called whenever an actor is activated.
        /// An actor is activated the first time any of its methods are invoked.
        /// </summary>
        protected override async Task OnActivateAsync()
        {
            ActorEventSource.Current.ActorMessage(this, this.GetActorId().GetStringId());
        }
    }
}
