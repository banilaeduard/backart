using RepositoryContract.Tickets;

namespace RepositoryContract.Tasks
{
    public interface ITaskRepository
    {
        public Task InsertNew(TaskEntry entry);

        public Task<TaskEntry> InsertFromTicketEntries(TicketEntity[] tickets);
    }
}
