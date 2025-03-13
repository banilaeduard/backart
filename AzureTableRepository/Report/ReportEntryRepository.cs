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

        public async Task<T> AddEntry<T>(T entity, string tableName) where T : class, ITableEntity
        {
            await tableStorageService.Insert(entity, tableName);
            CacheManager.Bust(tableName, true, null);
            return entity;
        }

        public async Task<List<LocationMap>> GetLocationMapPathEntry(string partitionKey, Func<LocationMap, bool> pred)
        {
            return tableStorageService.Query<LocationMap>(t => t.PartitionKey == partitionKey).Where(pred).ToList();
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