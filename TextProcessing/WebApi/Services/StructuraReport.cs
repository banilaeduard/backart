using EntityDto.Reports;
using RepositoryContract;
using RepositoryContract.ProductCodes;
using RepositoryContract.Report;
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
            }
        }

        public async Task<Stream> GenerateReport(string reportName, string locationCode, IVisitable<KeyValuePair<string, int>> data, Dictionary<string, object> ctx)
        {
            await PrepareReportName(reportName);
            var template = await _reportEntryRepository.GetReportTemplate(locationCode, reportName);

            var templatePath = Path.Combine(_connectionSettings.SqlQueryCache, template.TemplateName);
            var currentTemplateWriter = _templateDocumentWriter.SetTemplate(templatePath);

            var kvps = new List<KeyValuePair<string, int>>();
            ContextMap contextMap = new(ctx);
            data.Accept(currentTemplateWriter, kvps, contextMap);

            var countMap = new Dictionary<string, (int count, Report report)>();
            foreach (var kvp in kvps)
            {
                if (rootProductMapping.ContainsKey(kvp.Key))
                {
                    var reports = rootProductMapping[kvp.Key];
                    foreach (var report in reports)
                    {
                        if (!countMap.ContainsKey(report.Display))
                        {
                            countMap[report.Display] = (0, report);
                        }
                        countMap[report.Display] = (countMap[report.Display].count + kvp.Value, countMap[report.Display].report);
                    }
                }
            }
            var items = countMap.Select(t => t.Value).OrderBy(t => t.report.Order).ToList();
            string currentGroup = items.First().report.Group;

            List<string[]> values = [];

            contextMap.ResetIndex();
            foreach (var item in items)
            {
                if (item.report.Group != currentGroup)
                {
                    values.Add(["", "", "", ""]);
                    currentGroup = item.report.Group;
                }

                values.Add([contextMap.IncrementIndex().ToString(), item.report.Display, item.report.UM, item.count.ToString()]);
            }

            currentTemplateWriter.WriteToTable(reportName, [..values]);
            return currentTemplateWriter.GetStream();
        }
    }
}
