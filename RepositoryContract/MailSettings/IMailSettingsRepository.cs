namespace RepositoryContract.MailSettings
{
    public interface IMailSettingsRepository
    {
        Task<IQueryable<MailSettingEntry>> GetMailSetting(string source);
        Task<IQueryable<MailSourceEntry>> GetMailSource();
    }
}
