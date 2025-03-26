namespace EntityDto.ExternalReferenceGroup
{
    public class ExternalReferenceGroup: IdentityEquality<ExternalReferenceGroup>, ITableEntryDto
    {
        public int G_Id { get; set; }
        public string TableName { get; set; }
        public string PartitionKey { get; set; }
        public string RowKey { get; set; }
        public string ExternalGroupId { get; set; }
        public DateTime Date { get; set; }
        public int Ref_count { get; set; }
        public string EntityType { get; set; }
        public int Id { get; set; }
        public DateTimeOffset? Timestamp { get; set; }
    }
}
