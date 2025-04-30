using RepositoryContract;
using RepositoryContract.Report;
using WordDocumentServices;

namespace WebApi.Services
{
    public class SimpleReport
    {
        ILogger _logger;
        ITemplateDocumentWriter _templateDocumentWriter;
        ConnectionSettings _connectionSettings;
        IReportEntryRepository _reportEntryRepository;

        public SimpleReport(
            ITemplateDocumentWriter templateDocumentWriter,
            ConnectionSettings connectionSettings,
            IReportEntryRepository reportEntryRepository,
            ILogger<SimpleReport> logger
            )
        {
            _templateDocumentWriter = templateDocumentWriter;
            _connectionSettings = connectionSettings;
            _reportEntryRepository = reportEntryRepository;
            _logger = logger;
        }
        public async Task<Stream> GetSimpleReport<T>(string reportName, string reportLocation, IVisitable<T> model, Dictionary<string, string>? ctx)
        {
            var template = await _reportEntryRepository.GetReportTemplate(reportLocation, reportName);
            if (template == null)
            {
                _logger.LogError(@$"Missing report for {reportLocation} - {reportName}");
                return Stream.Null;
            }

            var writer = _templateDocumentWriter.SetTemplate(Path.Combine(_connectionSettings.SqlQueryCache, template.TemplateName));

            model.Accept(writer, null, new(ctx));
            return writer.GetStream();
        }
    }
}
