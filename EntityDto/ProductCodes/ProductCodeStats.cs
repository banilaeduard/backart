namespace EntityDto.ProductCodes
{
    public class ProductCodeStats : IdentityEquality<ProductCodeStats>, ITableEntryDto
    {
        public string PartitionKey { get; set; }
        public string RowKey { get; set; }
        public DateTimeOffset? Timestamp { get; set; }
        public string ProductPartitionKey { get; set; }
        public string ProductRowKey { get; set; }
        public string StatsPartitionKey { get; set; }
        public string StatsRowKey { get; set; }
        public int Id { get; set; }
    }
}
