using Azure;
using Azure.Data.Tables;
using EntityDto;
using System.Diagnostics.CodeAnalysis;

namespace RepositoryContract.Report
{
    public class ReportEntry : EntityDto.Reports.Report, ITableEntity, ITableEntryDto<ReportEntry>
    {
        public ETag ETag { get; set; }

        public bool Equals(ReportEntry? x, ReportEntry? y)
        {
            return base.Equals(x, y);
        }

        public int GetHashCode([DisallowNull] ReportEntry obj)
        {
            return base.GetHashCode(obj);
        }
    }
}
