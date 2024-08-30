using Microsoft.ServiceFabric.Services.Remoting;

namespace YahooFeeder
{
    public interface IYahooFeeder: IService
    {
        public Task Get();
    }
}
