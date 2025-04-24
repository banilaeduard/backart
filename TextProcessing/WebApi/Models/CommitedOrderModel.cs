using EntityDto.CommitedOrders;
using WordDocumentServices;

namespace WebApi.Models
{
    public class CommitedOrderModel : IVisitable<KeyValuePair<string, int>>
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
                DetaliiLinie = $@"{entry.DetaliiLinie}{(string.IsNullOrWhiteSpace(entry.DetaliiLinie) ? "" : " - ")}{entry.DetaliiDoc}",
                DataDocumentBaza = entry.DataDocumentBaza?.ToUniversalTime(),
            };
        }

        public void Accept(ITemplateDocumentWriter visitor, List<KeyValuePair<string, int>> contextItems, ContextMap context)
        {
            var found = contextItems.FindIndex(x => x.Key == CodProdus);
            if (found == -1) contextItems.Add(KeyValuePair.Create(CodProdus, Cantitate));
            else contextItems[found] = KeyValuePair.Create(CodProdus, contextItems[found].Value + Cantitate);
        }
    }
}
