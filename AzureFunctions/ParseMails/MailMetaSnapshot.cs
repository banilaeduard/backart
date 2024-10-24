using Azure;
using Azure.Data.Tables;

namespace ParseMails
{
    public class MailMetaSnapshot : ITableEntity
    {
        public string PartitionKey { get; set; }
        public string RowKey { get; set; }
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }
        public int Validity { get; set; }
        public int Id { get; set; }
        public string Folders { get; set; }
        public string From { get; set; }
        public string FoundInFolder { get; set; }
    }
}
