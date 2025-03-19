using Azure;
using Azure.Data.Tables;
using EntityDto.Reports;
using System.Diagnostics.CodeAnalysis;

namespace RepositoryContract.Report
{
    public class ReportTemplateEntry : ReportTemplate, ITableEntity
    {
        public ETag ETag { get; set; }

        public bool Equals(ReportTemplateEntry? x, ReportTemplateEntry? y)
        {
            return base.Equals(x, y);
        }

        public int GetHashCode([DisallowNull] ReportTemplateEntry obj)
        {
            return base.GetHashCode(obj);
        }
    }
}
