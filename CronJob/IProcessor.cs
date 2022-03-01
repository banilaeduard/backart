using System.Threading.Tasks;

namespace CronJob
{
    public interface IProcessor<T>
    {
        Task<bool> shouldProcess(T message, string id);
        Task process(T message, string id);
    }
}
