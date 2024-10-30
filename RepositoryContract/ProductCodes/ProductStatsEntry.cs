using Azure;
using Azure.Data.Tables;

namespace RepositoryContract.ProductCodes
{
    public class ProductStatsEntry : ITableEntity
    {
        public string PartitionKey { get; set; }
        public string RowKey { get; set; }
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }
        public string PropertyName { get; set; }
        public string PropertyValue { get; set; }
        public string PropertyType { get; set; }

        public ProductStatsEntry Shallowcopy()
        {
            return (ProductStatsEntry)MemberwiseClone();
        }
    }
}
