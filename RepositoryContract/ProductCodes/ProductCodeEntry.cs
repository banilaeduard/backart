using Azure;
using Azure.Data.Tables;

namespace AzureServices
{
    public class ProductCodeEntry: ITableEntity
    {
        public string CodeDisplay { get; set; }
        public string PartitionKey { get; set; }
        public string RowKey { get; set; }
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }
    }
}
