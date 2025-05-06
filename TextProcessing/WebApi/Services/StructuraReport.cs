using AzureTableRepository.Report;
using EntityDto.Reports;
using RepositoryContract;
using RepositoryContract.ProductCodes;
using RepositoryContract.Report;
using RepositoryServices;
using ServiceImplementation;
using WordDocumentServices;

namespace WebApi.Services
{
    public class StructuraReport
    {
        ILogger _logger;
        ITemplateDocumentWriter _templateDocumentWriter;
        ConnectionSettings _connectionSettings;
        StructuraReportWriter _templateWriter;
        IReportEntryRepository _reportEntryRepository;

        public StructuraReport(
            ITemplateDocumentWriter templateDocumentWriter,
            ConnectionSettings connectionSettings,
            IReportEntryRepository reportEntryRepository,
            StructuraReportWriter templateWriter,
            ILogger<StructuraReport> logger
            )
        {
            _templateDocumentWriter = templateDocumentWriter;
            _connectionSettings = connectionSettings;
            _reportEntryRepository = reportEntryRepository;
            _templateWriter = templateWriter;
            _logger = logger;
        }

        public async Task<Stream> GenerateReport(string reportName, string locationCode, IVisitable<Dictionary<string, int>> data, Dictionary<string, string>? ctx = null)
        {
            var template = await _reportEntryRepository.GetReportTemplate(locationCode, reportName);

            var templatePath = Path.Combine(_connectionSettings.SqlQueryCache, template.TemplateName);
            var currentTemplateWriter = _templateDocumentWriter.SetTemplate(TempFileHelper.CreateTempFile(templatePath));

            var contextMap = new ContextMap(ctx);
            var items = await _templateWriter.GenerateReport(reportName, currentTemplateWriter, data, contextMap);

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
    }
}
