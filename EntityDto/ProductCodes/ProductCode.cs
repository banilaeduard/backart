namespace EntityDto.ProductCodes
{
    public class ProductCode : IdentityEquality<ProductCode>, ITableEntryDto
    {
        public string Name { get; set; }
        public string Bar { get; set; }
        public string Code { get; set; }
        public string ParentCode { get; set; }
        public string RootCode { get; set; }
        public int Level { get; set; }
        public string PartitionKey { get; set; }
        public string RowKey { get; set; }
        public DateTimeOffset? Timestamp { get; set; }
        public int Id { get; set; }
    }
}
