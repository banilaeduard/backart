namespace RepositoryContract.Tasks
{
    public interface ITaskRepository
    {
        public Task<TaskEntry> UpdateTask(TaskEntry task);
        public Task<TaskEntry> SaveTask(TaskEntry task);
        public Task<IList<TaskEntry>> GetTasks(TaskInternalState taskStatus);
        public Task<IList<ExternalReferenceEntry>> GetExternalReferences();
        public Task DeleteTask(int Id);
    }

    public enum TaskInternalState
    {
        All = 3,
        Closed = 1,
        Open = 2
    }
}
