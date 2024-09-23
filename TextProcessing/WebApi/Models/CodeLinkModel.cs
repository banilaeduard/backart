namespace WebApi.Models
{
    public class CodeLinkModel
    {
        public int Id { get; set; }
        public string CodeDisplay { get; set; }
        public string CodeValue { get; set; }
        public string? CodeValueFormat { get; set; }
        //public ICollection<CodeLinkModel> Ancestors { get; set; }
        public ICollection<CodeLinkModel>? Children { get; set; }
        public bool? isRoot { get; set; }
    }
}
