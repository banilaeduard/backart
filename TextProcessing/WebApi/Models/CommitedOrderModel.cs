using EntityDto.CommitedOrders;
using RepositoryContract.Cfg;
using RepositoryContract.ProductCodes;
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
        public List<CategoryValue> Categories { get; set; } = new List<CategoryValue>();
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

        public CommitedOrderModel SetCategories(List<ProductCodeStatsEntry>? productLink, List<ProductStatsEntry>? productStats, List<CategoryEntity>? categories, string PartnerName)
        {
            Categories = new();
            foreach (var c in categories?.Where(c => c.PartitionKey == PartnerName) ?? [])
            {
                var itemLink = productLink?.FirstOrDefault(x => x.PartitionKey == $@"{PartnerName}_${c.CategoryName}" && x.RowKey == (PartnerItemKey ?? CodProdus));
                var itemStat = productStats?.FirstOrDefault(ps => ps.PartitionKey == itemLink?.StatsPartitionKey && ps.RowKey == itemLink?.StatsRowKey);
                Categories.Add(new CategoryValue
                {
                    CategoryName = c.CategoryName,
                    PartitionKey = c.PartitionKey,
                    RowKey = c.RowKey,
                    Value = itemStat?.PropertyValue?.ToString()
                });
            }
            return this;
        }

        public void Accept(ITemplateDocumentWriter visitor, Dictionary<string, int> contextItems, ContextMap context)
        {
            if (contextItems.ContainsKey(CodProdus)) contextItems[CodProdus] += Cantitate;
        }
    }
}

