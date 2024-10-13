using RepositoryContract.Tickets;

namespace RepositoryContract.Tasks
{
    public interface ITaskRepository
    {
        public Task<TaskEntry> SaveTask(TaskEntry task);
        public Task<IList<TaskEntry>> GetActiveTasks();
        public Task DeleteTask(int Id);
    }
}
