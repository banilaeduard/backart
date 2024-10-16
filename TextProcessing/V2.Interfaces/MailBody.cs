using RepositoryContract;

namespace V2.Interfaces
{
    public class MailBody
    {
        public TableEntityPK TableEntity { get; set; }
        public string Path { get; set; }
    }
}
