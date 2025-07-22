namespace EntityDto.CommitedOrders
{
    public class Order : IdentityEquality<Order>, ITableEntryDto
    {
        public Order() { }

        [MapExcel(1, type = typeof(string))]
        public int DocId
        {
            get => Get<int>(nameof(DocId));
            set => Set(nameof(DocId), value);
        }

        [MapExcel(2)]
        public string? DetaliiDoc
        {
            get => Get<string>(nameof(DetaliiDoc));
            set => Set(nameof(DetaliiDoc), value);
        }

        [MapExcel(3)]
        public DateTime? DataDoc
        {
            get => Get<DateTime?>(nameof(DataDoc));
            set => Set(nameof(DataDoc), value);
        }

        [MapExcel(5)]
        public string NumePartener
        {
            get => Get<string>(nameof(NumePartener));
            set => Set(nameof(NumePartener), value);
        }

        [MapExcel(6)]
        public string CodLocatie
        {
            get => Get<string>(nameof(CodLocatie));
            set => Set(nameof(CodLocatie), value);
        }

        [MapExcel(7)]
        public string NumeLocatie
        {
            get => Get<string>(nameof(NumeLocatie));
            set => Set(nameof(NumeLocatie), value);
        }

        [MapExcel(8)]
        public string NumarComanda
        {
            get => Get<string>(nameof(NumarComanda));
            set => Set(nameof(NumarComanda), value);
        }

        [MapExcel(9)]
        public string CodArticol
        {
            get => Get<string>(nameof(CodArticol));
            set => Set(nameof(CodArticol), value);
        }

        [MapExcel(10)]
        public string NumeArticol
        {
            get => Get<string>(nameof(NumeArticol));
            set => Set(nameof(NumeArticol), value);
        }

        [MapExcel(11)]
        public int CantitateTarget
        {
            get => Get<int>(nameof(CantitateTarget));
            set => Set(nameof(CantitateTarget), value);
        }

        [MapExcel(12)]
        public int Cantitate
        {
            get => Get<int>(nameof(Cantitate));
            set => Set(nameof(Cantitate), value);
        }

        [MapExcel(15)]
        public string? DetaliiLinie
        {
            get => Get<string>(nameof(DetaliiLinie));
            set => Set(nameof(DetaliiLinie), value);
        }

        public bool? HasChildren
        {
            get => Get<bool?>(nameof(HasChildren));
            set => Set(nameof(HasChildren), value);
        }

        public string StatusName
        {
            get => Get<string>(nameof(StatusName));
            set => Set(nameof(StatusName), value);
        }

        public int Id
        {
            get => Get<int>(nameof(Id));
            set => Set(nameof(Id), value);
        }

        public DateTime? DueDate
        {
            get => Get<DateTime?>(nameof(DueDate));
            set => Set(nameof(DueDate), value);
        }

        public string? PartnerItemKey
        {
            get => Get<string>(nameof(PartnerItemKey));
            set => Set(nameof(PartnerItemKey), value);
        }
    }
}