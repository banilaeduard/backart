using EntityDto.Reports;
using RepositoryContract;
using RepositoryContract.ProductCodes;
using RepositoryContract.Report;
using ServiceImplementation;
using WebApi.Models;
using WordDocumentServices;

namespace WebApi.Services
{
    public class StructuraReport
    {
        ILogger _logger;
        ITemplateDocumentWriter _templateDocumentWriter;
        ConnectionSettings _connectionSettings;
        IReportEntryRepository _reportEntryRepository;
        IProductCodeRepository _productCodesRepository;

        private string _reportName;
        private List<Report> reportEntries;
        private Dictionary<string, Report[]> rootProductMapping = new();
        private Dictionary<string, string> rootProductNames = new();

        public StructuraReport(
            ITemplateDocumentWriter templateDocumentWriter,
            ConnectionSettings connectionSettings,
            IReportEntryRepository reportEntryRepository,
            IProductCodeRepository productCodesRepository,
            ILogger<StructuraReport> logger
            )
        {
            _templateDocumentWriter = templateDocumentWriter;
            _connectionSettings = connectionSettings;
            _reportEntryRepository = reportEntryRepository;
            _productCodesRepository = productCodesRepository;
            _logger = logger;
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

        public async Task<Stream> GenerateReport(string reportName, string locationCode, IVisitable<Dictionary<string, int>> data, Dictionary<string, string>? ctx = null)
        {
            var template = await _reportEntryRepository.GetReportTemplate(locationCode, reportName);
            await PrepareReportName(template.ReportName);

            var templatePath = Path.Combine(_connectionSettings.SqlQueryCache, template.TemplateName);
            var currentTemplateWriter = _templateDocumentWriter.SetTemplate(TempFileHelper.CreateTempFile(templatePath));

            var contextMap = new ContextMap(ctx);
            var items = await GenerateReport(reportName, currentTemplateWriter, data, contextMap);

            string currentGroup = items.First().ReportInner.Group;

            List<string[]> values = [];

            contextMap.ResetIndex();
            foreach (var item in items.Where(t => t.Count > 0))
            {
                if (item.ReportInner.Group != currentGroup)
                {
                    values.Add(["", "", "", ""]);
                    currentGroup = item.ReportInner.Group;
                }

                values.Add([contextMap.IncrementIndex().ToString(), item.ReportInner.Display, item.ReportInner.UM, item.Count.ToString()]);
            }

            currentTemplateWriter.WriteToTable(reportName, [.. values]);
            return currentTemplateWriter.GetStream();
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
                                countMap[report.Display] = new ReportModel(report, 0, report.Display);
                            }
                            countMap[report.Display].Count += (report.Quantity ?? 1) * kvp.Value;
                        }
                        else
                        {
                            if (!countMap.ContainsKey(rootProductNames[kvp.Key]))
                            {
                                countMap[rootProductNames[kvp.Key]] = new ReportModel(report, 0, rootProductNames[kvp.Key]);
                            }
                            countMap[rootProductNames[kvp.Key]].Count += (report.Quantity ?? 1) * kvp.Value;
                        }
                    }
                }
            }

            return [.. countMap.Select(t => t.Value).OrderBy(t => t.ReportInner.Order)];
        }
    }
}
