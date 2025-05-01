using EntityDto.Reports;

namespace WebApi.Models
{
    public class ReportModel
    {
        public ReportModel(Report reportInner, int count, string mathchingProduct)
        {
            ReportInner = reportInner;
            Count = count;
            MathchingProduct = mathchingProduct;
        }

        public Report ReportInner { get; set; }
        public int Count { get; set; }
        public string MathchingProduct { get; set; }
    }
}
