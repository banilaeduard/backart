using System.Fabric;
using MetadataService.Interfaces;
using Microsoft.ServiceFabric.Data;
using Microsoft.ServiceFabric.Data.Collections;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Microsoft.ServiceFabric.Services.Remoting.V2.FabricTransport.Runtime;
using Microsoft.ServiceFabric.Services.Runtime;
using V2.Interfaces;

namespace MetadataService
{
    /// <summary>
    /// An instance of this class is created for each service replica by the Service Fabric runtime.
    /// </summary>
    internal sealed class MetadataService : StatefulService, IMetadataServiceFabric
    {
        public MetadataService(StatefulServiceContext context)
            : base(context)
        { }

        // **CREATE or UPDATE**
        public async Task ClearAndSetDataAsync(KeyValuePairList kvp, string collectionName)
        {
            var myDictionary = await StateManager.GetOrAddAsync<IReliableDictionary<string, string>>(collectionName);

            using (var tx = StateManager.CreateTransaction())
            {
                await myDictionary.ClearAsync();
                foreach (var kvp2 in kvp.Items)
                {
                    await myDictionary.SetAsync(tx, kvp2.Key, kvp2.Value);
                }

                await tx.CommitAsync();
            }
        }


        // **DELETE**
        public async Task DeleteDataAsync(string collectionKey = "items")
        {
            var myDictionary = await StateManager.GetOrAddAsync<IReliableDictionary<string, string>>(collectionKey);

            using (var tx = StateManager.CreateTransaction())
            {
                await myDictionary.ClearAsync();
                await tx.CommitAsync();
            }
        }

        public async Task<List<string>> GetAllCollectionKeys()
        {
            List<string> allKeys = new List<string>();

            using (ITransaction tx = StateManager.CreateTransaction())
            {
                var enumerator = StateManager.GetAsyncEnumerator();

                while (await enumerator.MoveNextAsync(CancellationToken.None))
                {
                    // Get the name of the reliable state
                    Uri stateUri = enumerator.Current.Name;
                    string dictionaryName = Uri.UnescapeDataString(stateUri.Segments[^1]);

                    try
                    {
                        var dictionary = await StateManager.TryGetAsync<IReliableDictionary<string, string>>(dictionaryName);
                        if (dictionary.HasValue)
                        {
                            allKeys.Add(dictionaryName);
                        }
                    }
                    catch (Exception ex)
                    {
                    }
                }
            }

            return allKeys;
        }

        // **GET ALL DATA**
        public async Task<KeyValuePairList> GetAllDataAsync(string collectionKey = "items")
        {
            var myDictionary = await StateManager.GetOrAddAsync<IReliableDictionary<string, string>>(collectionKey);
            var result = new KeyValuePairList();

            using (var tx = StateManager.CreateTransaction())
            {

                var enumerable = await myDictionary.CreateEnumerableAsync(tx);
                using (var enumerator = enumerable.GetAsyncEnumerator())
                {
                    while (await enumerator.MoveNextAsync(CancellationToken.None))
                    {
                        result.Items.Add(KeyValuePair.Create(enumerator.Current.Key, enumerator.Current.Value));
                    }
                }
            }
            return result;
        }

        protected override IEnumerable<ServiceReplicaListener> CreateServiceReplicaListeners()
        {
            return [
                  new ServiceReplicaListener((context) =>
                            new FabricTransportServiceRemotingListener(context, this))
            ];
        }
    }
}
