namespace EntityDto
{
    public class IdentityEquality<T> where T : ITableEntryDto
    {
        public X Shallowcopy<X>() where X : T
        {
            return (X)this.MemberwiseClone();
        }

        public virtual bool Equals(T x, T y)
        {
            if (ReferenceEquals(x, y)) return true;

            if (ReferenceEquals(x, null) || ReferenceEquals(y, null))
                return false;

            if (x.PartitionKey == y.PartitionKey && x.RowKey == y.RowKey && x.Id == y.Id)
            {
                return true;
            }

            return false;
        }

        public virtual int GetHashCode(T other)
        {
            return GetStableHashCode(other.PartitionKey) ^ GetStableHashCode(other.RowKey) ^ GetStableHashCode(other.Id.ToString());
        }

        public static int GetStableHashCode(string? str)
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
