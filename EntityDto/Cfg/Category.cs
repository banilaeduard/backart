namespace EntityDto.Config
{
    public class Category : IdentityEquality<Category>, ITableEntryDto
    {
        public int Id { get; set; }
        public string PartitionKey { get; set; }
        public string RowKey { get; set; }
        public DateTimeOffset? Timestamp { get; set; }
        public string CategoryName { get; set; }
        public string ObjectType { get; set; }
    }
}
