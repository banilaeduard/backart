using Azure;
using Azure.Data.Tables;
using EntityDto;

namespace AzureServices
{
    internal class ComandaVanzareAzEntry : ComandaVanzare, ITableEntity
    {
        public string PartitionKey { get; set; }
        public string RowKey { get; set; }
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }

        public static ComandaVanzareAzEntry create(ComandaVanzare entry)
        {
            return new ComandaVanzareAzEntry()
            {
                Cantitate = entry.Cantitate,
                Timestamp = DateTime.Now,
                CodArticol = entry.CodArticol,
                CodLocatie = entry.CodLocatie,
                DataDoc = entry.DataDoc,
                DetaliiDoc = entry.DetaliiDoc,
                DetaliiLinie = entry.DetaliiLinie,
                DocId = entry.DocId,
                HasChildren = entry.HasChildren,
                NumarComanda = entry.NumarComanda,
                NumeArticol = entry.NumeArticol,
                NumeLocatie = entry.NumeLocatie,
                NumePartener = entry.NumePartener,
                RowKey = entry.DocId.ToString(),
            };
        }
    }
}
