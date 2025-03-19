
namespace EntityDto.Reports
{
    public class LocationMap : IdentityEquality<LocationMap>, ITableEntryDto
    {
        public string PartitionKey { get; set; }
        public string RowKey { get; set; }
        public DateTimeOffset? Timestamp { get; set; }
        public string Folder { get; set; }
        public string Location { get; set; }
        public int Id { get; set; }
    }
}
