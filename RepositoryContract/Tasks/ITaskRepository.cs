using RepositoryContract.Tickets;

namespace RepositoryContract.Tasks
{
    public interface ITaskRepository
    {
        public Task<IList<TaskEntry>> GetActiveTasks();

        public Task<TaskEntry> InsertFromTicketEntries(TicketEntity[] tickets);

        public Task DeleteTask(int Id);
    }
}
