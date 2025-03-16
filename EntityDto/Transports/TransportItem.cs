namespace EntityDto.Transports
{
    public class TransportItem : IdentityEquality<TransportItem>, ITableEntryDto<TransportItem>
    {
        public int ItemId { get; set; }
        public int DocumentType { get; set; }
        public string ItemName { get; set; }
        public DateTime Created { get; set; }
        public int TransportId { get; set; }
        public string ExternalItemId { get; set; }
        public string ExternalItemId2 { get; set; }
        public int Id { get { return ItemId; } set { ItemId = value; } }
        public string PartitionKey { get; set; }
        public string RowKey { get; set; }
        public DateTimeOffset? Timestamp { get; set; }
    }
}
