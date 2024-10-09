using System.ComponentModel.DataAnnotations.Schema;

namespace RepositoryContract.Tasks
{
    public class TaskEntry
    {
        public int Id { get; set; }

        [ColumnAttribute("Name")]
        public string Name { get; set; }
        public string Details { get; set; }
        public DateTime Created { get; set; }
    }
}
