namespace EntityDto
{
    public class AddMailToTask
    {
        public string PartitionKey { get; set; }
        public string RowKey { get; set; }
        public string ThreadId { get; set; }
        public DateTime Date { get; set; }
        public string TableName { get; set; }
        public string LocationRowKey { get; set; }
        public string LocationPartitionKey { get; set; }
    }
}
