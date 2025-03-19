namespace EntityDto.Reports
{
    public class Report : IdentityEquality<Report>, ITableEntryDto
    {
        public string PartitionKey { get; set; }
        public string RowKey { get; set; }
        public DateTimeOffset? Timestamp { get; set; }
        public string Group { get; set; }
        public int Order { get; set; }
        public string Display { get; set; }
        public string UM { get; set; }
        public string FindBy { get; set; }
        public int Level { get; set; }
        public int Id { get; set; }
    }
}
