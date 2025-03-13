using Azure.Data.Tables;

namespace RepositoryContract.Report
{
    public interface IReportEntryRepository
    {
        Task<List<ReportEntry>> GetReportEntry(string reportName);
        Task<ReportTemplate> GetReportTemplate(string codLocatie, string reportName);
        Task<List<LocationMap>> GetLocationMapPathEntry(string partitionKey, Func<LocationMap, bool> pred);
        Task<T> AddEntry<T>(T entity, string tableName) where T : class, ITableEntity;
    }
}
