namespace EntityDto
{
    public class DispozitieLivrare
    {
        public DispozitieLivrare() { CodProdus = ""; NumeProdus = ""; }

        [MapExcel(1, 2)]
        public DateTime DataDocument { get; set; }
        [MapExcel(34)]
        public string? CodLocatie { get; set; }
        [MapExcel(35)]
        public string? NumeLocatie { get; set; }
        [MapExcel(6, 2, srcType: typeof(long))]
        public string NumarIntern { get; set; }
        [MapExcel(1)]
        public string CodProdus { get; set; }
        [MapExcel(2)]
        public string NumeProdus { get; set; }
        [MapExcel(5)]
        public int Cantitate { get; set; }
        [MapExcel(32)]
        public string? NumeCodificare { get; set; }
        [MapExcel(33)]
        public string CodEan { get; set; }
        public string PartitionKey { get; set; }
        public string RowKey { get; set; }
        public string NumarComanda { get; set; }
        public string AggregatedFileNmae { get; set; }
        public DateTimeOffset? Timestamp { get; set; }
        public string StatusName { get; set; }
        public string DetaliiLinie { get; set; }
        public string DetaliiDoc { get; set; }
        public DateTime DataDocumentBaza { get; set; }
    }
}
