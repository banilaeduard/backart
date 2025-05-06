using EntityDto.Reports;
using Microsoft.Extensions.Logging;
using RepositoryContract.ProductCodes;
using RepositoryContract.Report;
using RepositoryServices.Models;
using WordDocumentServices;

namespace RepositoryServices
{
    public class StructuraReportWriter
    {
        IReportEntryRepository _reportEntryRepository;
        IProductCodeRepository _productCodesRepository;

        private string _reportName;
        private List<Report> reportEntries;
        private Dictionary<string, Report[]> rootProductMapping = new();
        private Dictionary<string, string> rootProductNames = new();

        public StructuraReportWriter(
            IReportEntryRepository reportEntryRepository,
            IProductCodeRepository productCodesRepository
            )
        {
            _reportEntryRepository = reportEntryRepository;
            _productCodesRepository = productCodesRepository;
        }

        private async Task PrepareReportName(string reportName)
        {
            if (_reportName == reportName) return;
            _reportName = reportName;
            reportEntries = (await _reportEntryRepository.GetReportEntry(reportName)).Cast<Report>().ToList();
            var productHierarchy = (await _productCodesRepository.GetProductCodes(c =>
                                                                        reportEntries.Any(re => c.Code.Contains(re.FindBy) && c.Level == re.Level))
                                                                 ).GroupBy(x => x.RootCode).ToList();
            foreach (var prods in productHierarchy)
            {
                var product = prods.First();
                rootProductMapping[product.RootCode] = reportEntries
                    .Where(re => prods.Any(p => p.Code.Contains(re.FindBy) && p.Level == re.Level))
                    .ToArray();
                rootProductNames[product.RootCode] = product.Name;
            }
        }

        public async Task<List<ReportModel>> GenerateReport(string reportName, ITemplateDocumentWriter currentTemplateWriter, IVisitable<Dictionary<string, int>> data, ContextMap? contextMap = null)
        {
            await PrepareReportName(reportName);

            var kvps = new Dictionary<string, int>();
            foreach (var kvp in rootProductMapping)
            {
                kvps[kvp.Key] = 0;
            }

            data.Accept(currentTemplateWriter, kvps, contextMap ?? new ContextMap(null));

            var countMap = new Dictionary<string, ReportModel>();
            foreach (var kvp in kvps)
            {
                if (rootProductMapping.ContainsKey(kvp.Key))
                {
                    var reports = rootProductMapping[kvp.Key];
                    foreach (var report in reports)
                    {
                        if (!string.IsNullOrWhiteSpace(report.Display))
                        {
                            if (!countMap.ContainsKey(report.Display))
                            {
                                countMap[report.Display] = new ReportModel(report, 0, report.Display, report.Display);
                            }
                            countMap[report.Display].Count += (report.Quantity ?? 1) * kvp.Value;
                        }
                        else
                        {
                            if (!countMap.ContainsKey(kvp.Key))
                            {
                                countMap[kvp.Key] = new ReportModel(report, 0, kvp.Key, rootProductNames[kvp.Key]);
                            }
                            countMap[kvp.Key].Count += (report.Quantity ?? 1) * kvp.Value;
                        }
                    }
                }
            }

            return [.. countMap.Select(t => t.Value).OrderBy(t => t.ReportInner.Order)];
        }
    }
}
