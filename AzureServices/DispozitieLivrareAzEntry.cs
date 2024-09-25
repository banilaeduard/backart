using Azure;
using Azure.Data.Tables;
using EntityDto;

namespace AzureServices
{
    public class DispozitieLivrareAzEntry : DispozitieLivrare, ITableEntity
    {
        public string PartitionKey { get; set; }
        public string RowKey { get; set; }
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }
        public string AggregatedFileNmae { get; set; }

        public static DispozitieLivrareAzEntry create(DispozitieLivrare entry, int cantitate)
        {
            return new DispozitieLivrareAzEntry()
            {
                Cantitate = cantitate,
                CodEan = entry.CodEan,
                CodLocatie = entry.CodLocatie,
                CodProdus = entry.CodProdus,
                CodProdus2 = entry.CodProdus2,
                NumarIntern = entry.NumarIntern,
                NumeCodificare = entry.NumeCodificare,
                NumeLocatie = entry.NumeLocatie,
                NumeProdus = entry.NumeProdus,
                Timestamp = DateTime.Now.ToUniversalTime(),
            };
        }
    }
}