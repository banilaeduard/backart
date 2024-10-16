using Azure.Data.Tables;
using Azure;

namespace RepositoryContract
{
    public class TableEntityPK : IEqualityComparer<ITableEntity>, ITableEntity
    {
        public string PartitionKey { get; set; }
        public string RowKey { get; set; }
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }
        public static TableEntityPK From(string partitionKey, string rowKey)
        {
            return new TableEntityPK()
            {
                PartitionKey = partitionKey,
                RowKey = rowKey
            };
        }
        public bool Equals(ITableEntity x, ITableEntity y)
        {
            if (ReferenceEquals(x, y)) return true;

            if (ReferenceEquals(x, null) || ReferenceEquals(y, null))
                return false;

            if (x.PartitionKey == y.PartitionKey && x.RowKey == y.RowKey)
            {
                return true;
            }

            return false;
        }

        public int GetHashCode(ITableEntity other)
        {
            return other.PartitionKey.GetHashCode() ^ other.RowKey.GetHashCode();
        }
    }
}
