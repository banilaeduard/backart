using Azure.Data.Tables;
using Azure;
namespace RepositoryContract
{
    public class TableEntityPK : IEqualityComparer<TableEntityPK>
    {
        public TableEntityPK() { }
        public string PartitionKey { get; set; }
        public string RowKey { get; set; }
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }
        public int Id { get; set; }

        public static TableEntityPK From(string partitionKey, string rowKey)
        {
            return new TableEntityPK()
            {
                PartitionKey = partitionKey,
                RowKey = rowKey
            };
        }

        public static IEqualityComparer<T> GetComparer<T>() where T : TableEntityPK
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
            return GetStableHashCode(other.PartitionKey) ^ GetStableHashCode(other.RowKey);
        }

        public int GetHashCode(TableEntityPK other)
        {
            return GetStableHashCode(other.PartitionKey) ^ GetStableHashCode(other.RowKey);
        }

        private static int GetStableHashCode(string? str)
        {
            if (str == null) return 0;
            unchecked
            {
                int hash1 = 5381;
                int hash2 = hash1;

                for (int i = 0; i < str.Length && str[i] != '\0'; i += 2)
                {
                    hash1 = ((hash1 << 5) + hash1) ^ str[i];
                    if (i == str.Length - 1 || str[i + 1] == '\0')
                        break;
                    hash2 = ((hash2 << 5) + hash2) ^ str[i + 1];
                }

                return hash1 + (hash2 * 1566083941);
            }
        }
    }
}
