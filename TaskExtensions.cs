namespace WebApi
{
    using System.Threading.Tasks;
    using Microsoft.Extensions.Logging;
    static class TaskExtensions
    {
        public static void Forget(this Task task, ILogger logger)
        {
            task.ContinueWith(
                err => { logger.LogError(err.Id, err.Exception, ""); },
                TaskContinuationOptions.OnlyOnFaulted);
        }
    }
}