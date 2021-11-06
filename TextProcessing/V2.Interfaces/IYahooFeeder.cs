using Microsoft.ServiceFabric.Services.Remoting;
using RepositoryContract;
using V2.Interfaces;

namespace YahooFeeder
{
    public interface IYahooFeeder : IService
    {
        public Task Get();
        public Task<MailBody[]> DownloadAll(TableEntityPK[] uids);
    }
}
