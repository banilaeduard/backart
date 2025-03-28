namespace EntityDto.ExternalReferenceGroup
{
    public class ExternalReferenceRelationship
    {
        public int Id { get; set; }
        public int ExternalReferenceId { get; set; }
        public int InternalDatabaseId { get; set; }
        public int ReferenceTypeId { get; set; }
        public DateTime Created { get; set; }
    }
}
