using Microsoft.ServiceFabric.Services.Remoting;
using Microsoft.ServiceFabric.Services.Remoting.FabricTransport;

[assembly: FabricTransportServiceRemotingProvider(RemotingListenerVersion = RemotingListenerVersion.V2, RemotingClientVersion = RemotingClientVersion.V2)]
namespace MailExtrasExtractor
{
    public interface IMailExtrasExtractor : IService
    {
        Task<Extras> Parse(string body);
    }
}
