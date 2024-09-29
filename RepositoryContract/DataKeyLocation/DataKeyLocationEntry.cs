using Azure;
using Azure.Data.Tables;

namespace RepositoryContract.DataKeyLocation
{
    public class DataKeyLocationEntry: ITableEntity
    {
        public string LocationName { get; set; }
        public string LocationCode { get; set; }
        public string TownName { get; set; }
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
