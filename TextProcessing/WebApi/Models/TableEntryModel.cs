namespace WebApi.Models
{
    public class TableEntryModel
    {
        public string PartitionKey { get; set; }
        public string RowKey { get; set; }
        public string? TableName { get; set; }
    }
}
