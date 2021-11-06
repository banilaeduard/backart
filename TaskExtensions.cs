using System.Threading.Tasks;
static class TaskExtensions
{
    public static void Forget(this Task task)
    {
        task.ContinueWith(
            t => { },
            TaskContinuationOptions.OnlyOnFaulted);
    }
}