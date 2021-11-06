using Azure;
using Azure.Data.Tables;
using EntityDto;

namespace RepositoryContract.Orders
{
    public class ComandaVanzareEntry : ComandaVanzare, ITableEntity
    {
        public ETag ETag { get; set; }

        private static ComandaComparer comparer = new(false);

        public static ComandaVanzareEntry create(ComandaVanzare entry)
        {
            var it = new ComandaVanzareEntry()
            {
                Cantitate = entry.Cantitate,
                Timestamp = DateTime.Now.ToUniversalTime(),
                CodArticol = entry.CodArticol,
                CodLocatie = entry.CodLocatie,
                DataDoc = entry.DataDoc?.ToUniversalTime(),
                DetaliiDoc = entry.DetaliiDoc,
                DetaliiLinie = entry.DetaliiLinie,
                DocId = entry.DocId,
                HasChildren = entry.HasChildren,
                NumarComanda = entry.NumarComanda,
                NumeArticol = entry.NumeArticol,
                NumeLocatie = entry.NumeLocatie,
                NumePartener = entry.NumePartener,
                CantitateTarget = entry.CantitateTarget,
                PartitionKey = PKey(entry),
            };
            it.RowKey = comparer.GetHashCode(it).ToString();
            return it;
        }

        public static string PKey(ComandaVanzare entry)
        {
            return $"{(entry.DocId / 100)}";
        }

        public static IEqualityComparer<ComandaVanzareEntry> GetEqualityComparer(bool includeQ = false)
        {
            return new ComandaComparer(includeQ);
        }

        public static string GetProgressTableName()
        {
            return $"{typeof(ComandaVanzareEntry).Name}Progress";
        }

        internal class ComandaComparer : IEqualityComparer<ComandaVanzareEntry>
        {
            bool includeQ;
            public ComandaComparer(bool includeQ) { this.includeQ = includeQ; }
            public bool Equals(ComandaVanzareEntry x, ComandaVanzareEntry y)
            {
                if (ReferenceEquals(x, y)) return true;

                if (ReferenceEquals(x, null) || ReferenceEquals(y, null))
                    return false;

                if (x.NumePartener == y.NumePartener && x.CodLocatie == y.CodLocatie && x.DocId == y.DocId
                    && x.CodArticol == y.CodArticol && x.DetaliiDoc == y.DetaliiDoc && x.DetaliiLinie == y.DetaliiLinie && x.NumarComanda == y.NumarComanda
                    && (!includeQ || x.Cantitate == y.Cantitate))
                {
                    return true;
                }

                return false;
            }

            public int GetHashCode(ComandaVanzareEntry other)
            {
                // if (Object.ReferenceEquals(number, null)) return 0;
                int hash1 = GetStableHashCode(other.NumePartener);
                int hash2 = GetStableHashCode(other.CodLocatie);
                int hash3 = GetStableHashCode(other.DocId.ToString());
                int hash5 = GetStableHashCode(other.CodArticol);
                int hash6 = GetStableHashCode(other.DetaliiDoc);
                int hash7 = GetStableHashCode(other.DetaliiLinie);
                int hash9 = GetStableHashCode(other.NumarComanda);
                int hash8 = GetStableHashCode(other.Cantitate.ToString());

                if (includeQ)
                    return hash1 ^ hash2 ^ hash3 ^ hash5 ^ hash6 ^ hash7 ^ hash9 ^ hash8;
                return hash1 ^ hash2 ^ hash3 ^ hash5 ^ hash6 ^ hash7 ^ hash9;
            }
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