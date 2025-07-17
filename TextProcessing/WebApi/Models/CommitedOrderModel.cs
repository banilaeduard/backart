using EntityDto.CommitedOrders;
using WordDocumentServices;

namespace WebApi.Models
{
    public class CommitedOrderModel : IVisitable<Dictionary<string, int>>
    {
        public string CodProdus { get; set; }
        public string NumeProdus { get; set; }
        public int Cantitate { get; set; }
        public string? PartitionKey { get; set; }
        public string? RowKey { get; set; }
        public string? NumarComanda { get; set; }
        public string? DetaliiLinie { get; set; }
        public DateTime? DataDocumentBaza { get; set; }
        public string? NumarIntern { get; set; }
        public int? Greutate { get; set; }

        public string? PartnerItemKey { get; set; }

        public static CommitedOrderModel create(CommitedOrder entry, int cantitate, int greutate)
        {
            return new CommitedOrderModel()
            {
                Cantitate = cantitate,
                Greutate = greutate,
                CodProdus = entry.CodProdus,
                NumeProdus = entry.NumeProdus,
                PartitionKey = entry.NumarIntern,
                RowKey = entry.RowKey,
                NumarIntern = entry.NumarIntern,
                NumarComanda = entry.NumarComanda,
                PartnerItemKey = entry.PartnerItemKey,
                DetaliiLinie = $@"{entry.DetaliiLinie}{(string.IsNullOrWhiteSpace(entry.DetaliiLinie) ? "" : " - ")}{entry.DetaliiDoc}",
                DataDocumentBaza = entry.DataDocumentBaza?.ToUniversalTime(),
            };
        }

        public void Accept(ITemplateDocumentWriter visitor, Dictionary<string, int> contextItems, ContextMap context)
        {
            if (contextItems.ContainsKey(CodProdus)) contextItems[CodProdus] += Cantitate;
        }
    }
}
