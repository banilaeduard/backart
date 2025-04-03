using Azure.Data.Tables;
using AzureServices;
using ProjectKeys;
using RepositoryContract.Report;

namespace AzureTableRepository.Report
{
    public class ReportEntryRepository : IReportEntryRepository
    {
        TableStorageService tableStorageService;

        public ReportEntryRepository()
        {
            tableStorageService = new TableStorageService();
        }

        public async Task<LocationMapEntry> AddEntry(LocationMapEntry entity, string tableName)
        {
            await tableStorageService.Insert(entity, tableName);
            return entity;
        }

        public async Task<List<LocationMapEntry>> GetLocationMapPathEntry(string partitionKey, Func<LocationMapEntry, bool> pred)
        {
            return tableStorageService.Query<LocationMapEntry>(t => t.PartitionKey == partitionKey).Where(pred).ToList();
        }

        public async Task<List<ReportEntry>> GetReportEntry(string reportName)
        {
            return tableStorageService.Query<ReportEntry>(t => true).ToList();
        }

        public async Task<ReportTemplateEntry> GetReportTemplate(string codLocatie, string reportName)
        {
            var tableName = typeof(ReportTemplateEntry).Name;
            TableClient tableClient = new(Environment.GetEnvironmentVariable(KeyCollection.StorageConnection), tableName, new TableClientOptions());
            tableClient.CreateIfNotExists();
            var resp = tableClient.GetEntityIfExists<ReportTemplateEntry>(codLocatie, reportName);
            return resp.HasValue ? resp.Value! : null;
        }
    }
}