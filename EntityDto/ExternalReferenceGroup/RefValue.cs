namespace EntityDto.ExternalReferenceGroup
{
    public class RefValue
    {
        public int RefValueId { get; set; }
        public int ReferenceTypeId { get; set; }
        public string ReferenceValue { get; set; }
        public int? RelationshipId { get; set; }
        public int? InternalRelationshipId { get; set; }
    }
}
