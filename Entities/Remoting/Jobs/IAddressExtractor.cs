using Microsoft.ServiceFabric.Services.Remoting;
using Microsoft.ServiceFabric.Services.Remoting.FabricTransport;

[assembly: FabricTransportServiceRemotingProvider(RemotingListenerVersion = RemotingListenerVersion.V2, RemotingClientVersion = RemotingClientVersion.V2)]
namespace Entities.Remoting.Jobs
{
    public interface IAddressExtractor : IService
    {
        Task<string[]> Parse(string body);
    }
}
