using System.Diagnostics.CodeAnalysis;

namespace RepositoryContract.Tasks
{
    public class TaskEntry : EntityDto.Tasks.TaskWork
    {
        public List<TaskActionEntry> Actions { get; set; }
        public List<ExternalReferenceEntry> ExternalReferenceEntries { get; set; }

        public static TaskEntry From(TaskEntry entry, IList<TaskActionEntry> actions, IList<ExternalReferenceEntry>? externalReferenceEntries)
        {
            entry.Actions = [.. actions.Where(a => a.TaskId == entry.Id)];
            entry.ExternalReferenceEntries = [.. (externalReferenceEntries ?? []).Where(e => e.TaskId == entry.Id)];
            return entry;
        }

        public bool Equals(TaskEntry? x, TaskEntry? y)
        {
            return base.Equals(x, y);
        }

        public int GetHashCode([DisallowNull] TaskEntry obj)
        {
            return base.GetHashCode(obj);
        }
    }
}
