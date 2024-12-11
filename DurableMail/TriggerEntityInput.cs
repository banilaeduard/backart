using RepositoryContract;

namespace MailReaderDurable
{
    internal class TriggerEntityInput: TableEntityPK
    {
        public string QueueMessageId { get; set; }
        public string QueueMessageReceipt { get; set; }
        public uint Uid { get; set; }
        public uint Validity { get; set; }
    }
}
