using Azure;
using Azure.Data.Tables;
using EntityDto;

namespace RepositoryContract.CommitedOrders
{
    public class DispozitieLivrareEntry : DispozitieLivrare, ITableEntity
    {
        public ETag ETag { get; set; }

        public static DispozitieLivrareEntry create(DispozitieLivrare entry, int cantitate)
        {
            return new DispozitieLivrareEntry()
            {
                Cantitate = cantitate,
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
            };
        }
    }
}