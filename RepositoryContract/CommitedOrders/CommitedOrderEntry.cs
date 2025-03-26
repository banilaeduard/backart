using Azure;
using Azure.Data.Tables;
using EntityDto;
using EntityDto.CommitedOrders;
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
                TransportDate = entry.TransportDate,
                TransportId = entry.TransportId
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
    }
}
