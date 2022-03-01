namespace core
{
    using System;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Logging;
    public static class TaskExtensions
    {
        public static void Forget(this Task task, ILogger logger, Action action = null)
        {
            task.ContinueWith(rez =>
            {
                if (action != null)
                {
                    action();
                }
            }).ContinueWith(
                err => { logger.LogError(err.Id, err.Exception, ""); },
                TaskContinuationOptions.OnlyOnFaulted);
        }
    }
}