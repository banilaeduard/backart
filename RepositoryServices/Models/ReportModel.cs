using EntityDto.Reports;

namespace RepositoryServices.Models
{
    public class ReportModel
    {
        public ReportModel(Report reportInner, int count, string mathchingProduct, string displayName)
        {
            ReportInner = reportInner;
            Count = count;
            MathchingProduct = mathchingProduct;
            DisplayName = displayName;
        }

        public Report ReportInner { get; set; }
        public int Count { get; set; }
        public string MathchingProduct { get; set; }
        public string DisplayName { get; set; }
    }
}
