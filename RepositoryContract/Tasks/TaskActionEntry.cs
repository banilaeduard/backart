using EntityDto.Tasks;
using System.Diagnostics.CodeAnalysis;

namespace RepositoryContract.Tasks
{
    public class TaskActionEntry: TaskAction
    {
        public int Id { get; set; }
        public int TaskId { get; set; }
        public string Description { get; set; }
        public DateTime Created { get; set; }

        public bool Equals(TaskActionEntry? x, TaskActionEntry? y)
        {
            return base.Equals(x, y);
        }

        public int GetHashCode([DisallowNull] TaskActionEntry obj)
        {
            return base.GetHashCode(obj);
        }
    }
}
