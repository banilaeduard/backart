namespace RepositoryContract.Report
{
    public interface IReportEntryRepository
    {
        Task<List<ReportEntry>> GetReportEntry(string reportName);
        Task<ReportTemplateEntry> GetReportTemplate(string codLocatie, string reportName);
        Task<List<LocationMapEntry>> GetLocationMapPathEntry(string partitionKey, Func<LocationMapEntry, bool> pred);
        Task<LocationMapEntry> AddEntry(LocationMapEntry entity, string tableName);
    }
}
