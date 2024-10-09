namespace RepositoryContract.Tasks
{
    public class ExternalReferenceEntry
    {
        public int Id { get; set; }
        public int TaskId { get; set; }
        public int TaskActionId { get; set; }
        public string TableReferenceName { get; set; }
        public string EntityName { get; set; }
        public string PartitionKey { get; set; }
        public string RowKey { get; set; }
        public bool IsRemoved { get; set; }
        public DateTime Created { get; set; }
    }
}
