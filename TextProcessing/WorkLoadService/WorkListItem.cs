namespace WorkLoadService
{
    public class WorkListItem
    {
        public const string Order = "Order";
        public const string Commited = "Dispozitie";

        public string StatusName { get; set; }
        public DateTime? Delivered { get; set; }
        public string TransportId { get; set; }
        public string DetaliiDoc { get; set; }
        public string DocId { get; set; }
        public string Tip { get; set; }
        public DateTime? DataDoc { get; set; }
        public string NumePartener { get; set; }
        public string CodLocatie { get; set; }
        public string NumeLocatie { get; set; }
        public string NumarComanda { get; set; }
        public string CodArticol { get; set; }
        public string NumeArticol { get; set; }
        public DateTime? DueDate { get; set; }
        public int Cantitate { get; set; }
        public string DetaliiLinie { get; set; }
        public string TransportStatus { get; set; }
    }
}
