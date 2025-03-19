namespace EntityDto.MailSettings
{
    public class MailSetting : IdentityEquality<MailSetting>, ITableEntryDto
    {
        public string From { get; set; }
        public string Folders { get; set; }
        public string PartitionKey { get; set; }
        public string RowKey { get; set; }
        public string Source { get; set; }
        public DateTimeOffset? Timestamp { get; set; }
        public int Id { get; set; }
    }
}
