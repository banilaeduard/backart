namespace RepositoryContract.Tasks
{
    public interface ITaskRepository
    {
        public Task<TaskEntry> UpdateTask(TaskEntry task);
        public Task<TaskEntry> SaveTask(TaskEntry task);
        public Task<IList<TaskEntry>> GetActiveTasks();
        public Task<IList<ExternalReferenceEntry>> GetExternalReferences();
        public Task DeleteTask(int Id);
    }
}
