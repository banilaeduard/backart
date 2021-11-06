using Azure;
using Azure.Data.Tables;

namespace RepositoryContract.DataKeyLocation
{
    public class DataKeyLocationEntry: DataKeyLocationBase, ITableEntity
    {
        public string PartitionKey { get; set; }
        public string RowKey { get; set; }
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }

        public DataKeyLocationEntry Shallowcopy()
        {
            return (DataKeyLocationEntry)this.MemberwiseClone();
        }
    }
}
