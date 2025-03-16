using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Actors.Client;
using ServiceInterface.Storage;
using MetadataService.Interfaces;

namespace AzureFabricServices
{
    public class FabricMetadataService : IMetadataService
    {
        public async Task<ILeaseClient> GetLease(string fName, params string[] args)
        {
            return GetSemaphore(fName = args != null ? string.Format(fName, args) : fName);
        }

        public async Task<IDictionary<string, string>> GetMetadata(string fName, params string[] args)
        {
            fName = args != null ? string.Format(fName, args) : fName;
            var proxy = ActorProxy.Create<IMetadataActor>(new ActorId(fName), new Uri("fabric:/TextProcessing/MetadataActorService"));
            return await proxy.GetMetadata(fName);
        }

        public async Task SetMetadata(string fName, string? leaseId, IDictionary<string, string> metadata = null, params string[] args)
        {
            fName = args != null ? string.Format(fName, args) : fName;
            var proxy = ActorProxy.Create<IMetadataActor>(new ActorId(fName), new Uri("fabric:/TextProcessing/MetadataActorService"));
            await proxy.SetMetadata(fName, metadata);
        }

        private static WrapLock GetSemaphore(string name)
        {
            var semaphoreName = $@"{nameof(FabricMetadataService)}-{name}";
            return new(new(0, 1, semaphoreName), semaphoreName);
        }
    }

    public class WrapLock : IDisposable, ILeaseClient
    {
        Semaphore semaphore;
        string leaseId;
        public WrapLock(Semaphore semaphore, string fname)
        {
            this.semaphore = semaphore;
            this.leaseId = fname;
        }

        public string LeaseId => leaseId!;

        public async Task<ILeaseClient> Acquire(TimeSpan time)
        {
            semaphore.WaitOne(time);
            return this;
        }

        public void Dispose()
        {
            semaphore.Release();
            semaphore.Dispose();
            semaphore = null;
        }
    }
}
