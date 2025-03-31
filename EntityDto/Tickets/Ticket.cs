namespace EntityDto.Tickets
{
    public class Ticket : IdentityEquality<Ticket>, ITableEntryDto
    {
        public int Uid { get; set; }
        public int Validity { get; set; }
        public string? From { get; set; }
        public string? NrComanda { get; set; }
        public string? Description { get; set; }
        public DateTime CreatedDate { get; set; }
        public string PartitionKey { get; set; }
        public string RowKey { get; set; }
        public DateTimeOffset? Timestamp { get; set; }
        public string MessageId { get; set; }
        public string? InReplyTo { get; set; }
        public string? Locations { get; set; }
        public string? References { get; set; }
        public string? Sender { get; set; }
        public string OriginalBodyPath { get; set; }
        public string? Subject { get; set; }
        public string EmailId { get; set; }
        public string ThreadId { get; set; }
        public string LocationCode { get; set; }
        public string LocationPartitionKey { get; set; }
        public string LocationRowKey { get; set; }
        public bool HasAttachments { get; set; }
        public string FoundInFolder { get; set; }
        public string CurrentFolder { get; set; }
        public int Id { get; set; }
    }
}
