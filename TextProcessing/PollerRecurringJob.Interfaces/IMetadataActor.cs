using Microsoft.ServiceFabric.Actors;

namespace V2_1.Interfaces
{
    public interface IMetadataActor : IActor
    {
        Task<IDictionary<string, string>> GetMetadata(string fName);

        Task SetMetadata(string fName, IDictionary<string, string> metadata);
    }
}
