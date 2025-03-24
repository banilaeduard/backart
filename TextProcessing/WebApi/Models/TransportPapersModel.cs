namespace WebApi.Models
{
    public class TransportPapersModel
    {
        public int TransportId { get; set; }
        public string DriverName { get; set; }
        public int CommitedDuplicates { get; set; }
        public int ComplaintsDuplicates { get; set; }
        public CommitedOrdersBase[]? CommitedOrders { get; set; }
        public ComplaintDocument[]? Complaints { get; set; }
        public bool? RegenerateDocuments { get; set; }
    }
}
