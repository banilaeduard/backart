using Azure;
using Azure.Data.Tables;
using EntityDto;
using EntityDto.CommitedOrders;
using RepositoryContract.Orders;
using System.Diagnostics.CodeAnalysis;

namespace RepositoryContract.CommitedOrders
{
    public class CommitedOrderEntry : CommitedOrder, ITableEntity, IEqualityComparer<CommitedOrderEntry>
    {
        public ETag ETag { get; set; }

        public static CommitedOrderEntry create(CommitedOrder entry, int cantitate, int greutate)
        {
            return new CommitedOrderEntry()
            {
                Cantitate = cantitate,
                Greutate = greutate,
                CodEan = entry.CodEan,
                CodLocatie = entry.CodLocatie,
                CodProdus = entry.CodProdus,
                NumarIntern = entry.NumarIntern,
                NumeCodificare = entry.NumeCodificare,
                NumeLocatie = entry.NumeLocatie,
                NumeProdus = entry.NumeProdus,
                Timestamp = DateTime.Now.ToUniversalTime(),
                PartitionKey = entry.NumarIntern,
                RowKey = Guid.NewGuid().ToString(),
                DataDocument = entry.DataDocument.ToUniversalTime(),
                NumarComanda = entry.NumarComanda,
                StatusName = entry.StatusName,
                DetaliiDoc = entry.DetaliiDoc,
                DetaliiLinie = entry.DetaliiLinie,
                DataDocumentBaza = entry.DataDocumentBaza?.ToUniversalTime(),
                Livrata = entry.Livrata,
                NumarAviz = entry.NumarAviz,
                DataAviz = entry.DataAviz,
                TransportStatus = entry.TransportStatus,
                TransportDate = entry.TransportDate?.ToUniversalTime(),
                TransportId = entry.TransportId,
                DueDate = entry.DueDate?.ToUniversalTime(),
                PartnerItemKey = entry.PartnerItemKey,
                NumePartener = entry.NumePartener,
            };
        }

        public bool Equals(CommitedOrderEntry? x, CommitedOrderEntry? y)
        {
            return base.Equals(x, y);
        }

        public int GetHashCode([DisallowNull] CommitedOrderEntry obj)
        {
            return base.GetHashCode(obj);
        }

        public static IEqualityComparer<CommitedOrderEntry> GetEqualityComparer()
        {
            return new CommitedOrderEntryComparer();
        }

        internal class CommitedOrderEntryComparer : IEqualityComparer<CommitedOrderEntry>
        {
            public CommitedOrderEntryComparer() { }
            public bool Equals(CommitedOrderEntry x, CommitedOrderEntry y)
            {
                if (ReferenceEquals(x, y)) return true;

                if (ReferenceEquals(x, null) || ReferenceEquals(y, null))
                    return false;

                if (x.NumePartener == y.NumePartener && x.CodLocatie == y.CodLocatie && x.NumarIntern == y.NumarIntern
                    && x.CodProdus == y.CodProdus && x.DetaliiDoc == y.DetaliiDoc && x.DetaliiLinie == y.DetaliiLinie && x.NumarComanda == y.NumarComanda
                    && x.Cantitate == y.Cantitate)
                {
                    return true;
                }

                return false;
            }

            public int GetHashCode(CommitedOrderEntry other)
            {
                // if (Object.ReferenceEquals(number, null)) return 0;
                int hash1 = IdentityEquality<CommitedOrderEntry>.GetStableHashCode(other.NumePartener);
                int hash2 = IdentityEquality<CommitedOrderEntry>.GetStableHashCode(other.CodLocatie);
                int hash3 = IdentityEquality<CommitedOrderEntry>.GetStableHashCode(other.NumarIntern.ToString());
                int hash5 = IdentityEquality<CommitedOrderEntry>.GetStableHashCode(other.CodProdus);
                int hash6 = IdentityEquality<CommitedOrderEntry>.GetStableHashCode(other.DetaliiDoc);
                int hash7 = IdentityEquality<CommitedOrderEntry>.GetStableHashCode(other.DetaliiLinie);
                int hash9 = IdentityEquality<CommitedOrderEntry>.GetStableHashCode(other.NumarComanda);
                int hash8 = IdentityEquality<CommitedOrderEntry>.GetStableHashCode(other.Cantitate.ToString());

                return hash1 ^ hash2 ^ hash3 ^ hash5 ^ hash6 ^ hash7 ^ hash9 ^ hash8;
            }
        }
    }
}
