using Azure.Data.Tables;
using Azure;

namespace RepositoryContract
{
    public class TableEntityPK : IEqualityComparer<ITableEntity>, ITableEntity, IEqualityComparer<TableEntityPK>
    {
        public TableEntityPK() { }
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

        public static IEqualityComparer<T> GetComparer<T>() where T: TableEntityPK, ITableEntity
        {
            return new TableEntityPK();
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

        public bool Equals(TableEntityPK? x, TableEntityPK? y)
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

        public int GetHashCode(TableEntityPK other)
        {
            return other.PartitionKey.GetHashCode() ^ other.RowKey.GetHashCode();
        }
    }
}
