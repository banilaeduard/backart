using Azure;
using Azure.Data.Tables;

namespace RepositoryContract.MailSettings
{
    public class MailSettingEntry : EntityDto.MailSettings.MailSetting, ITableEntity
    {
        public ETag ETag { get; set; }
    }
}
