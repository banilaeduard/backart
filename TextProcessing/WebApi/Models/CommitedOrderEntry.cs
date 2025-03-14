using EntityDto;

namespace WebApi.Models
{
    public class CommitedOrderEntry
    {
        public string CodProdus { get; set; }
        public string NumeProdus { get; set; }
        public int Cantitate { get; set; }
        public string PartitionKey { get; set; }
        public string? RowKey { get; set; }
        public string NumarComanda { get; set; }
        public string DetaliiLinie { get; set; }
        public DateTime? DataDocumentBaza { get; set; }
        public int? Greutate { get; set; }

        public static CommitedOrderEntry create(DispozitieLivrare entry, int cantitate, int greutate)
        {
            return new CommitedOrderEntry()
            {
                Cantitate = cantitate,
                Greutate = greutate,
                CodProdus = entry.CodProdus,
                NumeProdus = entry.NumeProdus,
                PartitionKey = entry.NumarIntern,
                RowKey = entry.RowKey,
                NumarComanda = entry.NumarComanda,
                DetaliiLinie = $@"{entry.DetaliiLinie}{(string.IsNullOrWhiteSpace(entry.DetaliiLinie) ? "" : " - ")}{entry.DetaliiDoc}",
                DataDocumentBaza = entry.DataDocumentBaza?.ToUniversalTime(),
            };
        }
    }
}
