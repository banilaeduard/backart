using Azure;
using Azure.Data.Tables;
using EntityDto;

namespace AzureServices
{
    public class ComandaVanzareAzEntry : ComandaVanzare, ITableEntity
    {
        public string PartitionKey { get; set; }
        public string RowKey { get; set; }
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }

        private static ComandaComparer comparer = new(false);

        public static ComandaVanzareAzEntry create(ComandaVanzare entry)
        {
            var it = new ComandaVanzareAzEntry()
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
                PartitionKey = PKey(entry),
            };
            it.RowKey = comparer.GetHashCode(it).ToString();
            return it;
        }

        public static string PKey(ComandaVanzare entry)
        {
            return $"{entry.NumePartener}";
        }

        public static IEqualityComparer<ComandaVanzareAzEntry> GetEqualityComparer(bool includeQ = false)
        {
            return new ComandaComparer(includeQ);
        }

        public static string GetProgressTableName()
        {
            return $"{typeof(ComandaVanzareAzEntry).Name}Progress";
        }

        internal class ComandaComparer : IEqualityComparer<ComandaVanzareAzEntry>
        {
            bool includeQ;
            public ComandaComparer(bool includeQ) { this.includeQ = includeQ; }
            public bool Equals(ComandaVanzareAzEntry x, ComandaVanzareAzEntry y)
            {
                if (ReferenceEquals(x, y)) return true;

                if (ReferenceEquals(x, null) || ReferenceEquals(y, null))
                    return false;

                if (x.NumePartener == y.NumePartener && x.CodLocatie == y.CodLocatie && x.DocId == y.DocId && x.DataDoc == y.DataDoc
                    && x.CodArticol == y.CodArticol && x.DetaliiDoc == y.DetaliiDoc && x.DetaliiLinie == y.DetaliiLinie && x.NumarComanda == y.NumarComanda
                    && (!includeQ || x.Cantitate == y.Cantitate))
                {
                    return true;
                }

                return false;
            }

            public int GetHashCode(ComandaVanzareAzEntry other)
            {
                // if (Object.ReferenceEquals(number, null)) return 0;
                int hash1 = other.NumePartener.GetHashCode();
                int hash2 = other.CodLocatie == null ? 0 : other.CodLocatie.GetHashCode();
                int hash3 = other.DocId.GetHashCode();
                int hash4 = other.DataDoc == null ? 0 : other.DataDoc.GetHashCode();
                int hash5 = other.CodArticol == null ? 0 : other.CodArticol.GetHashCode();
                int hash6 = other.DetaliiDoc == null ? 0 : other.DetaliiDoc.GetHashCode();
                int hash7 = other.DetaliiLinie == null ? 0 : other.DetaliiLinie.GetHashCode();
                int hash9 = other.NumarComanda == null ? 0 : other.NumarComanda.GetHashCode();
                int hash8 = other.Cantitate.GetHashCode();

                if (includeQ)
                    return hash1 ^ hash2 ^ hash3 ^ hash4 ^ hash5 ^ hash6 ^ hash7 ^ hash9 ^ hash8;
                return hash1 ^ hash2 ^ hash3 ^ hash4 ^ hash5 ^ hash6 ^ hash7 ^ hash9;
            }
        }
    }
}