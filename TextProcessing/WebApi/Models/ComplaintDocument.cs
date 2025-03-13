namespace WebApi.Models
{
    public class ComplaintDocument
    {
        public DateTime Date { get; set; }
        public string LocationName { get; set; }
        public string LocationCode { get; set; }
        public int NumarIntern { get; set; }
        public ComplaintEntry[] complaintEntries { get; set; }
    }

    public class ComplaintEntry
    {
        public string Description { get; set; }
        public string UM { get; set; }
        public string Quantity { get; set; }
        public string Observation { get; set; }
        public string? RefPartitionKey { get; set; }
        public string? RefRowKey { get; set; }
        public bool? CloseTask { get; set; }
    }
}
