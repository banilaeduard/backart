using Azure.Data.Tables;
using AzureServices;
using AzureTableRepository;

namespace RepositoryContract.Report
{
    public class ReportEntryRepository : IReportEntryRepository
    {
        TableStorageService tableStorageService;

        public ReportEntryRepository()
        {
            tableStorageService = new TableStorageService();
        }

        public async Task<List<ReportEntry>> GetReportEntry(string reportName)
        {
            return CacheManager.GetAll((from) => tableStorageService.Query<ReportEntry>(t => t.Timestamp > from).ToList()).Where(x => x.PartitionKey == reportName).OrderBy(x => x.Order).ToList();
        }

        public async Task<ReportTemplate> GetReportTemplate(string codLocatie, string reportName)
        {
            var tableName = typeof(ReportTemplate).Name;
            TableClient tableClient = new(Environment.GetEnvironmentVariable("storage_connection"), tableName, new TableClientOptions());
            tableClient.CreateIfNotExists();
            var resp = tableClient.GetEntityIfExists<ReportTemplate>(codLocatie, reportName);
            return resp.HasValue ? resp.Value! : null;
        }
    }
}