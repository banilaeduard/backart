using ServiceInterface.Storage;
using MetadataService.Interfaces;
using Microsoft.ServiceFabric.Services.Remoting.Client;
using Microsoft.ServiceFabric.Services.Remoting.V2.FabricTransport.Client;
using Microsoft.ServiceFabric.Services.Client;

namespace AzureFabricServices
{
    public class FabricMetadataService : IMetadataService
    {
        internal ServiceProxyFactory serviceProxy = new ServiceProxyFactory((c) =>
        {
            return new FabricTransportServiceRemotingClientFactory();
        });

        static readonly SemaphoreSlim _semaphoreSlim = new(0, 1);

        public async Task<ILeaseClient> GetLease(string fName, params string[] args)
        {
            return GetSemaphore(fName = args != null ? string.Format(fName, args) : fName);
        }

        public async Task<IDictionary<string, string>> GetMetadata(string fName, params string[] args)
        {
            fName = args != null ? string.Format(fName, args) : fName;

            return new Dictionary<string, string>((await GetService().GetAllDataAsync(fName)).Items);
        }

        public async Task SetMetadata(string fName, string? leaseId, IDictionary<string, string> metadata = null, params string[] args)
        {
            fName = args != null ? string.Format(fName, args) : fName;
            var kvpList = new V2.Interfaces.KeyValuePairList();
            kvpList.Items = metadata?.ToList() ?? [];
            await GetService().ClearAndSetDataAsync(kvpList, fName);
        }

        private IMetadataServiceFabric GetService()
        {
            var serviceUri = new Uri("fabric:/TextProcessing/MetadataService");
            return serviceProxy.CreateServiceProxy<IMetadataServiceFabric>(serviceUri, ServicePartitionKey.Singleton);
        }

        private static WrapLock GetSemaphore(string name)
        {
            var semaphoreName = $@"{nameof(FabricMetadataService)}-{name}";
            return new(_semaphoreSlim, semaphoreName);
        }
    }

    public class WrapLock : ILeaseClient
    {
        SemaphoreSlim semaphore;
        string leaseId;
        public WrapLock(SemaphoreSlim semaphore, string fname)
        {
            this.semaphore = semaphore;
            this.leaseId = fname;
        }

        public string LeaseId => leaseId!;

        public async Task<ILeaseClient> Acquire(TimeSpan time)
        {
            await semaphore.WaitAsync(time);
            return this;
        }

        public void Dispose()
        {
            try
            {
                semaphore?.Release();
            }
            catch {
            }
            semaphore = null;
        }
    }
}
