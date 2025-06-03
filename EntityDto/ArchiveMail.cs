namespace EntityDto
{
    public class ArchiveMail
    {
        public string PartitionKey { get; set; }
        public string RowKey { get; set; }
        public string FromTable { get; set; }
        public string ToTable { get; set; }
    }
}
