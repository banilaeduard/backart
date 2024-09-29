using Azure;
using Azure.Data.Tables;

namespace AzureSerRepositoryContract.ProductCodesvices
{
    public class ProductCodeEntry : ITableEntity
    {
        public string Name { get; set; }
        public string Bar { get; set; }
        public string Code { get; set; }
        public string ParentCode { get; set; }
        public string RootCode { get; set; }
        public int Level { get; set; }
        public string PartitionKey { get; set; }
        public string RowKey { get; set; }
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }

        public ProductCodeEntry Shallowcopy()
        {
            return (ProductCodeEntry)this.MemberwiseClone();
        }
    }
}
