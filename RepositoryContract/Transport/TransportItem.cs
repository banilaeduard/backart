namespace RepositoryContract.Transport
{
    public class TransportItem
    {
        public int ItemId { get; set; }
        public int DocumentType { get; set; }
        public string ItemName { get; set; }
        public DateTime Created { get; set; }
        public int TransportId { get; set; }
        public string ExternalItemId { get; set; }
        public string ExternalItemId2 { get; set; }
    }
}