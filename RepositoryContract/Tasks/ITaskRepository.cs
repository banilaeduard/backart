namespace RepositoryContract.Tasks
{
    public interface ITaskRepository
    {
        public Task<TaskEntry> UpdateTask(TaskEntry task);
        public Task<TaskEntry> SaveTask(TaskEntry task);
        public Task<IList<TaskEntry>> GetTasks(TaskInternalState taskStatus);
        public Task<IList<TaskEntry>> GetTasks(int[] taskIds);
        public Task MarkAsClosed(int[] taskIds);
        public Task DeleteTaskExternalRef(int taskId, string partitionKey, string rowKey);
        public Task DeleteTask(int Id);
        public Task AcceptExternalRef(int taskId, string partitionKey, string rowKey);
    }

    public enum TaskInternalState
    {
        All = 3,
        Closed = 1,
        Open = 2
    }
}
