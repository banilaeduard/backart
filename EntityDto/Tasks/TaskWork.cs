using System.ComponentModel.DataAnnotations.Schema;

namespace EntityDto.Tasks
{
    public class TaskWork : IdentityEquality<TaskWork>, ITableEntryDto
    {
        public int Id { get; set; }

        [ColumnAttribute("Name")]
        public string Name { get; set; }
        public string LocationCode { get; set; }
        public string Details { get; set; }
        public bool IsClosed { get; set; }
        public DateTime Created { get; set; }
        public DateTime TaskDate { get; set; }
        public string PartitionKey { get; set; }
        public string RowKey { get; set; }
        public DateTimeOffset? Timestamp { get; set; }
    }
}
