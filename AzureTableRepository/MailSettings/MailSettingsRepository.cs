using AzureServices;
using Microsoft.Extensions.Logging;
using RepositoryContract.MailSettings;

namespace AzureTableRepository.MailSettings
{
    public class MailSettingsRepository : IMailSettingsRepository
    {
        TableStorageService tableStorageService;

        public MailSettingsRepository(ILogger<TableStorageService> logger)
        {
            tableStorageService = new(logger);
        }

        public async Task<IQueryable<MailSettingEntry>> GetMailSetting()
        {
            return tableStorageService.Query<MailSettingEntry>(t => true).AsQueryable();
        }
    }
}
