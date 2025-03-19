namespace EntityDto.DataKeyLocations
{
    public class DataKeyLocation : IdentityEquality<DataKeyLocation>, ITableEntryDto
    {
        public int Id { get; set; }
        public string PartitionKey { get; set; }
        public string RowKey { get; set; }
        public DateTimeOffset? Timestamp { get; set; }
        public bool MainLocation { get; set; }
        public string LocationName { get; set; }
        public string LocationCode { get; set; }
        public string TownName { get; set; }
        public string ShortName { get; set; }
    }
}
