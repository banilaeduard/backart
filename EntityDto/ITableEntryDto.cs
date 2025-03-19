namespace EntityDto
{
    public interface ITableEntryDto
    {
        int Id { get; set; }
        string PartitionKey { get; set; }
        string RowKey { get; set; }
        DateTimeOffset? Timestamp { get; set; }
    }
}
