namespace WebApi.Entities
{
    public class CodeAttribute: ITenant
    {
        public string Id { get; set; }
        public string DisplayValue { get; set; }
        public string InnerValue { get; set; }
        public string Tag { get; set; }
        public string TenantId { get; set; }
    }
}
