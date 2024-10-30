using Azure;
using Azure.Data.Tables;

namespace RepositoryContract.ProductCodes
{
    public class ProductCodeStatsEntry : ITableEntity
    {
        public string PartitionKey { get; set; }
        public string RowKey { get; set; }
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }
        public string ProductPartitionKey { get; set; }
        public string ProductRowKey { get; set; }
        public string StatsPartitionKey { get; set; }
        public string StatsRowKey { get; set; }
    }
}
