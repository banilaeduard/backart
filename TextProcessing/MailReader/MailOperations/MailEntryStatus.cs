using Azure;
using Azure.Data.Tables;

namespace YahooTFeeder
{
    internal class MailEntryStatus : ITableEntity
    {
        public string PartitionKey { get; set; }
        public string RowKey { get; set; }
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }
        public DateTime LastFetch { get; set; }
        public string Folder { get; set; }
        public string From { get; set; }
    }
}
