namespace EntityDto.Tasks
{
    public class TaskAction : IdentityEquality<TaskAction>, ITableEntryDto
    {
        public int Id { get; set; }
        public int TaskId { get; set; }
        public string Description { get; set; }
        public DateTime Created { get; set; }
        public string PartitionKey { get; set; }
        public string RowKey { get; set; }
        public DateTimeOffset? Timestamp { get; set; }
    }
}
