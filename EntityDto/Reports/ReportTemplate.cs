namespace EntityDto.Reports
{
    public class ReportTemplate : IdentityEquality<ReportTemplate>, ITableEntryDto
    {
        public string PartitionKey { get; set; }
        public string RowKey { get; set; }
        public DateTimeOffset? Timestamp { get; set; }
        public string CodLocatie { get; set; }
        public string ReportName { get; set; }
        public string TemplateName { get; set; }
        public int Id { get; set; }
    }
}
