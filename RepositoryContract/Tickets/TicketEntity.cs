using Azure;
using Azure.Data.Tables;

namespace RepositoryContract.Tickets
{
    public class TicketEntity : ITableEntity
    {
        public int Uid { get; set; }
        public string? From { get; set; }
        public string? NrComanda { get; set; }
        public string TicketSource { get; set; }
        public string? Description { get; set; }
        public DateTime CreatedDate { get; set; }
        public string PartitionKey { get; set; }
        public string RowKey { get; set; }
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }
        public string MessageId { get; set; }
        public string? InReplyTo { get; set; }
        public string? Locations { get; set; }
        public string? References { get; set; }
        public string? Sender { get; set; }
        public string OriginalBodyPath { get; set; }
        public string? Subject { get; set; }
        public string EmailId { get; set; }
        public string ThreadId { get; set; }
        public bool IsDeleted { get; set; }
        public string ContentId { get; set; }
    }
}