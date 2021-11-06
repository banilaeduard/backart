namespace RepositoryContract.MailSettings
{
    public interface IMailSettingsRepository
    {
        Task<IQueryable<MailSettingEntry>> GetMailSetting();
    }
}
