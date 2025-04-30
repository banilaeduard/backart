using EntityDto.Reports;

namespace WebApi.Models
{
    public class ReportModel
    {
        public ReportModel(Report reportInner, int count)
        {
            ReportInner = reportInner;
            Count = count;
        }

        public Report ReportInner { get; set; }
        public int Count { get; set; }
    }
}
