using Azure;
using Azure.Data.Tables;

namespace RepositoryContract.Tickets
{
    public class TicketEntity: ITableEntity
    {
        public string From { get; set; }
        public string NrComanda { get; set; }
        public string TicketSource { get; set; }
        public string Description { get; set; }
        public DateTime CreatedDate { get; set; }
        public string PartitionKey { get; set; }
        public string RowKey { get; set; }
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }
        public string ResentFrom { get; set; }
        public string From2 { get; set; }
        public string ResentReplyTo { get; set; }
        public string MessageId { get; set; }
        public string InReplyTo { get; set; }
    }
}
