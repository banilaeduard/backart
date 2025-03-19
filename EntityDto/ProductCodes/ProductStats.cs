namespace EntityDto.ProductCodes
{
    public class ProductStats : IdentityEquality<ProductStats>, ITableEntryDto
    {
        public string PartitionKey { get; set; }
        public string RowKey { get; set; }
        public DateTimeOffset? Timestamp { get; set; }
        public string PropertyName { get; set; }
        public string PropertyValue { get; set; }
        public string PropertyType { get; set; }
        public string PropertyCategory { get; set; }
        public int Id { get; set; }
    }
}
