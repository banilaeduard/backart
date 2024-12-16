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
    }
}
