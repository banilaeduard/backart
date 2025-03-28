namespace EntityDto.ExternalReferenceGroup
{
    public class ExternalItem
    {
        public int ExternalId { get; set; }
        public int RefCount { get; set; }
        public DateTime Created { get; set; }
        public List<RefValue> ExternalReferenceValues { get; set; }
    }
}
