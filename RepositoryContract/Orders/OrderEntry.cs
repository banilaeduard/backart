using Azure;
using Azure.Data.Tables;
using EntityDto;
using EntityDto.CommitedOrders;

namespace RepositoryContract.Orders
{
    public class OrderEntry : Order, ITableEntity, IEqualityComparer<OrderEntry>
    {
        public ETag ETag { get; set; }

        private static ComandaComparer comparer = new(false);

        public static OrderEntry create(Order entry)
        {
            var it = new OrderEntry()
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

        public static string PKey(Order entry)
        {
            return $"{(entry.DocId / 100)}";
        }

        public static IEqualityComparer<OrderEntry> GetEqualityComparer(bool includeQ = false)
        {
            return new ComandaComparer(includeQ);
        }

        public static string GetProgressTableName()
        {
            return $"{typeof(OrderEntry).Name}Progress";
        }

        public new bool Equals(OrderEntry? x, OrderEntry? y)
        {
            return base.Equals(x, y);
        }

        public int GetHashCode(OrderEntry obj)
        {
            return base.GetHashCode();
        }

        internal class ComandaComparer : IEqualityComparer<OrderEntry>
        {
            bool includeQ;
            public ComandaComparer(bool includeQ) { this.includeQ = includeQ; }
            public bool Equals(OrderEntry x, OrderEntry y)
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

            public int GetHashCode(OrderEntry other)
            {
                // if (Object.ReferenceEquals(number, null)) return 0;
                int hash1 = IdentityEquality<OrderEntry>.GetStableHashCode(other.NumePartener);
                int hash2 = IdentityEquality<OrderEntry>.GetStableHashCode(other.CodLocatie);
                int hash3 = IdentityEquality<OrderEntry>.GetStableHashCode(other.DocId.ToString());
                int hash5 = IdentityEquality<OrderEntry>.GetStableHashCode(other.CodArticol);
                int hash6 = IdentityEquality<OrderEntry>.GetStableHashCode(other.DetaliiDoc);
                int hash7 = IdentityEquality<OrderEntry>.GetStableHashCode(other.DetaliiLinie);
                int hash9 = IdentityEquality<OrderEntry>.GetStableHashCode(other.NumarComanda);
                int hash8 = IdentityEquality<OrderEntry>.GetStableHashCode(other.Cantitate.ToString());

                if (includeQ)
                    return hash1 ^ hash2 ^ hash3 ^ hash5 ^ hash6 ^ hash7 ^ hash9 ^ hash8;
                return hash1 ^ hash2 ^ hash3 ^ hash5 ^ hash6 ^ hash7 ^ hash9;
            }
        }
    }
}
