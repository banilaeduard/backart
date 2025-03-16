using AzureServices;
using RepositoryContract.MailSettings;

namespace AzureTableRepository.MailSettings
{
    public class MailSettingsRepository : IMailSettingsRepository
    {
        TableStorageService tableStorageService;

        public MailSettingsRepository()
        {
            tableStorageService = new();
        }

        public async Task<IQueryable<MailSettingEntry>> GetMailSetting(string source)
        {
            return tableStorageService.Query<MailSettingEntry>(t => true).AsQueryable();
        }

        public async Task<IQueryable<MailSourceEntry>> GetMailSource()
        {
            return tableStorageService.Query<MailSourceEntry>(t => true).AsQueryable();
        }
    }
}
