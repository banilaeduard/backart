namespace EntityDto.CommitedOrders
{
    public class CommitedOrder : IdentityEquality<CommitedOrder>, ITableEntryDto
    {
        public CommitedOrder() { }

        [MapExcel(1, 2)]
        public DateTime DataDocument
        {
            get => Get<DateTime>(nameof(DataDocument));
            set => Set(nameof(DataDocument), value);
        }

        [MapExcel(34)]
        public string? CodLocatie
        {
            get => Get<string>(nameof(CodLocatie));
            set => Set(nameof(CodLocatie), value);
        }

        [MapExcel(35)]
        public string? NumeLocatie
        {
            get => Get<string>(nameof(NumeLocatie));
            set => Set(nameof(NumeLocatie), value);
        }

        [MapExcel(6, 2, srcType: typeof(long))]
        public string NumarIntern
        {
            get => Get<string>(nameof(NumarIntern));
            set => Set(nameof(NumarIntern), value);
        }

        [MapExcel(1)]
        public string CodProdus
        {
            get => Get<string>(nameof(CodProdus));
            set => Set(nameof(CodProdus), value);
        }

        [MapExcel(2)]
        public string NumeProdus
        {
            get => Get<string>(nameof(NumeProdus));
            set => Set(nameof(NumeProdus), value);
        }

        [MapExcel(5)]
        public int Cantitate
        {
            get => Get<int>(nameof(Cantitate));
            set => Set(nameof(Cantitate), value);
        }

        [MapExcel(32)]
        public string? NumeCodificare
        {
            get => Get<string>(nameof(NumeCodificare));
            set => Set(nameof(NumeCodificare), value);
        }

        [MapExcel(33)]
        public string CodEan
        {
            get => Get<string>(nameof(CodEan));
            set => Set(nameof(CodEan), value);
        }

        public string NumarComanda
        {
            get => Get<string>(nameof(NumarComanda));
            set => Set(nameof(NumarComanda), value);
        }

        public string AggregatedFileNmae
        {
            get => Get<string>(nameof(AggregatedFileNmae));
            set => Set(nameof(AggregatedFileNmae), value);
        }

        public string StatusName
        {
            get => Get<string>(nameof(StatusName));
            set => Set(nameof(StatusName), value);
        }

        public string DetaliiLinie
        {
            get => Get<string>(nameof(DetaliiLinie));
            set => Set(nameof(DetaliiLinie), value);
        }

        public string DetaliiDoc
        {
            get => Get<string>(nameof(DetaliiDoc));
            set => Set(nameof(DetaliiDoc), value);
        }

        public DateTime? DataDocumentBaza
        {
            get => Get<DateTime?>(nameof(DataDocumentBaza));
            set => Set(nameof(DataDocumentBaza), value);
        }

        public bool Livrata
        {
            get => Get<bool>(nameof(Livrata));
            set => Set(nameof(Livrata), value);
        }

        public int? NumarAviz
        {
            get => Get<int?>(nameof(NumarAviz));
            set => Set(nameof(NumarAviz), value);
        }

        public string TransportStatus
        {
            get => Get<string>(nameof(TransportStatus));
            set => Set(nameof(TransportStatus), value);
        }

        public DateTime? TransportDate
        {
            get => Get<DateTime?>(nameof(TransportDate));
            set => Set(nameof(TransportDate), value);
        }

        public int? TransportId
        {
            get => Get<int?>(nameof(TransportId));
            set => Set(nameof(TransportId), value);
        }

        public DateTime? DataAviz
        {
            get => Get<DateTime?>(nameof(DataAviz));
            set => Set(nameof(DataAviz), value);
        }

        public string? PartnerItemKey
        {
            get => Get<string>(nameof(PartnerItemKey));
            set => Set(nameof(PartnerItemKey), value);
        }

        public int? Greutate
        {
            get => Get<int?>(nameof(Greutate));
            set => Set(nameof(Greutate), value);
        }

        public int Id
        {
            get => Get<int>(nameof(Id));
            set => Set(nameof(Id), value);
        }

        public string NumePartener
        {
            get => Get<string>(nameof(NumePartener));
            set => Set(nameof(NumePartener), value);
        }

        public DateTime? DueDate
        {
            get => Get<DateTime?>(nameof(DueDate));
            set => Set(nameof(DueDate), value);
        }
    }
}
