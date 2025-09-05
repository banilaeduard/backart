namespace AzureServices
{
    public class EventGridMessage<T>
    {
        public string id { get; set; }
        public string subject { get; set; }
        public string eventType { get; set; }
        public string dataVersion { get; set; }
        public string metadataVersion { get; set; }
        public string topic { get; set; }
        public DateTime eventTime { get; set; }
        public T data { get; set; }
    }
}
