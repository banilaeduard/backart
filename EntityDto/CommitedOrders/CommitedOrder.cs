namespace EntityDto.CommitedOrders
{
    public class CommitedOrder : IdentityEquality<CommitedOrder>, ITableEntryDto
    {
        public CommitedOrder() { CodProdus = ""; NumeProdus = ""; }

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
        public string NumarComanda { get; set; }
        public string AggregatedFileNmae { get; set; }
        public string StatusName { get; set; }
        public string DetaliiLinie { get; set; }
        public string DetaliiDoc { get; set; }
        public DateTime? DataDocumentBaza { get; set; }
        public bool Livrata { get; set; }
        public int? NumarAviz { get; set; }
        public string TransportStatus { get; set; }
        public DateTime? TransportDate { get; set; }
        public int? TransportId { get; set; }
        public DateTime? DataAviz { get; set; }

        public int? Greutate { get; set; }
        public int Id { get; set; }
        public string PartitionKey { get; set; }
        public string RowKey { get; set; }
        public DateTimeOffset? Timestamp { get; set; }
    }
}
