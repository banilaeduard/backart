namespace WebApi.Models
{
    public class CodeLinkModel
    {
        public int Id { get; set; }
        public string CodeDisplay { get; set; }
        public string CodeValue { get; set; }
        public string? CodeBar { get; set; }
        public string ParentCode { get; set; }
        public string RootCode { get; set; }
        public int Level { get; set; }
    }
}
