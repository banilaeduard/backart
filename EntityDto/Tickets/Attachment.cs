namespace EntityDto.Tickets
{
    public class Attachment : IdentityEquality<Attachment>, ITableEntryDto
    {
        public string Data { get; set; }
        public string Title { get; set; }
        public string ContentType { get; set; }
        public string PartitionKey { get; set; }
        public string RowKey { get; set; }
        public string RefPartition { get; set; }
        public string RefKey { get; set; }
        public string ContentId { get; set; }
        public DateTimeOffset? Timestamp { get; set; }
        public int Id { get; set; }
        public bool? IsDeleted { get; set; }
    }
}
