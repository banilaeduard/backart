namespace EntityDto.ExternalReferenceGroup
{
    public class ReferenceType
    {
        public int ReferenceId { get; set; }
        public string EntityName { get; set; }
        public string ColumnName { get; set; }
        public int? ChainId { get; set; }
        public int SourceId { get; set; }
    }
}
