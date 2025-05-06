using Microsoft.ServiceFabric.Services.Remoting;
using WorkLoadService;

namespace V2.Interfaces
{
    public interface IWorkLoadService: IService
    {
        public Task Publish();
        public Task<Items> GetItems(string workerName);
    }
}
