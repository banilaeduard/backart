namespace WebApi.Models
{
    public class CodeLinkModel
    {
        public string Id { get; set; }
        public string CodeDisplay { get; set; }
        public string CodeValue { get; set; }
        public string? CodeBar { get; set; }
        public string ParentCode { get; set; }
        public string RootCode { get; set; }
        public int Level { get; set; }
        public string PartitionKey { get; set; }
        public string RowKey { get; set; }
        public ProductCodeStatsModel? ProductCodeStats { get; set; }
        public string ProductCodeStats_Id { get; set; }
    }
}
