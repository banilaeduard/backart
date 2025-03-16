using Microsoft.ServiceFabric.Services.Remoting;
using V2.Interfaces;

namespace MetadataService.Interfaces
{
    public interface IMetadataServiceFabric : IService
    {
        Task ClearAndSetDataAsync(KeyValuePairList kvp, string collectionKey);
        Task DeleteDataAsync(string collectionKey);
        Task<KeyValuePairList> GetAllDataAsync(string collectionKey);
        Task<List<string>> GetAllCollectionKeys();
    }
}
