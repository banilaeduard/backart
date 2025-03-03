using Azure;
using Azure.Data.Tables;

namespace RepositoryContract.Report
{
    public class ReportTemplate : ITableEntity
    {
        public string PartitionKey { get; set; }
        public string RowKey { get; set; }
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }

        public string CodLocatie { get; set; }
        public string ReportName { get; set; }
        public string TemplateName { get; set; }
    }
}
