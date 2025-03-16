namespace EntityDto
{
    public interface ITableEntryDto<T> : IEqualityComparer<T>
    {
        int Id { get; set; }
        string PartitionKey { get; set; }
        string RowKey { get; set; }
        DateTimeOffset? Timestamp { get; set; }
    }
}
