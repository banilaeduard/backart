using EntityDto.Reports;
using System.Runtime.Serialization;

namespace RepositoryServices.Models
{
    [DataContract]
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
        [DataMember]
        public int Count { get; set; }
        [DataMember]
        public string MathchingProduct { get; set; }
        [DataMember]
        public string DisplayName { get; set; }
    }
}
