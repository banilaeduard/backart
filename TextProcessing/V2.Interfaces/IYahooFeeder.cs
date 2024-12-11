using EntityDto;
using Microsoft.ServiceFabric.Services.Remoting;
using RepositoryContract;
using V2.Interfaces;

namespace YahooFeeder
{
    public interface IYahooFeeder : IService
    {
        public Task Batch(string sourceName);
        public Task ReadMails(string sourceName);
        public Task<MailBody[]> DownloadAll(TableEntityPK[] uids, string sourceName);
        public Task Move(MoveToMessage<TableEntityPK>[] messages, string sourceName);
    }
}
