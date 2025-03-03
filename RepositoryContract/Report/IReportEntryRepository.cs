namespace RepositoryContract.Report
{
    public interface IReportEntryRepository
    {
        Task<List<ReportEntry>> GetReportEntry(string reportName);
        Task<ReportTemplate> GetReportTemplate(string codLocatie, string reportName);
    }
}
