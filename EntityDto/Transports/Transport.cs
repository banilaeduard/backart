namespace EntityDto.Transports
{
    public class Transport : IdentityEquality<Transport>, ITableEntryDto
    {
        public int Id { get; set; }
        public string Description { get; set; }
        public string DriverName { get; set; }
        public string CarPlateNumber { get; set; }
        public int Distance { get; set; }
        public int FuelConsumption { get; set; }
        public string CurrentStatus { get; set; }
        public string ExternalItemId { get; set; }
        public DateTime Created { get; set; }
        public bool HasAttachments { get; set; }
        public DateTime? Delivered { get; set; }
        public string PartitionKey { get; set; }
        public string RowKey { get; set; }
        public DateTimeOffset? Timestamp { get; set; }
    }
}
