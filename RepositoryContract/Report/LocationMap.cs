using Azure.Data.Tables;
using Azure;

namespace RepositoryContract.Report
{
    public class LocationMap : ITableEntity
    {
        public string PartitionKey { get; set; }
        public string RowKey { get; set; }
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }

        public string Folder { get; set; }
        public string Location { get; set; }
    }
}
