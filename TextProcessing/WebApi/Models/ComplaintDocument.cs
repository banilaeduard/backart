namespace WebApi.Models
{
    public class ComplaintDocument
    {
        public DateTime Date { get; set; }
        public string LocationName { get; set; }
        public string LocationCode { get; set; }
        public List<ComplaintEntry> complaintEntries { get; set; }

        public string GetMd5(Func<string, string> getMd5)
        {
            List<string> list = new List<string>();
            foreach (var item in complaintEntries)
            {
                list.Add($"{item.Description}{item.UM}{item.Quantity}");
            }
            list.Add(LocationCode);
            list.Add(complaintEntries.Count().ToString());
            var stringToHash = string.Join("", list.Order());
            return getMd5(stringToHash.ToString());
        }
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
