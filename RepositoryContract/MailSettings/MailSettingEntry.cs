using Azure;
using Azure.Data.Tables;

namespace RepositoryContract.MailSettings
{
    public class MailSettingEntry: ITableEntity
    {
        public string From { get; set; }
        public string Folders { get; set; }
        public string PartitionKey { get; set; }
        public string RowKey { get; set; }
        public string Source { get; set; }
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }
    }
}
