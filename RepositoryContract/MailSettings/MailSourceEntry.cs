using Azure;
using Azure.Data.Tables;

namespace RepositoryContract.MailSettings
{
    public class MailSourceEntry : ITableEntity
    {
        public string UserName { get; set; }
        public string Password { get; set; }
        public int DaysBefore { get; set; }
        public string Host { get; set; }
        public int Port { get; set; }
        public bool UseSSL {  get; set; }
        public string Source { get; set; }
        public string PartitionKey { get; set; }
        public string RowKey { get; set; }
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }
    }
}
