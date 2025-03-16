using Azure;
using Azure.Data.Tables;
using EntityDto.MailSettings;

namespace RepositoryContract.MailSettings
{
    public class MailSourceEntry : MailSource, ITableEntity
    {
        public ETag ETag { get; set; }
    }
}
