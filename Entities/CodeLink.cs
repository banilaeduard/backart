using System.Collections.Generic;

namespace WebApi.Entities
{
    public class CodeLink
    {
        public int Id { get; set; }
        public string CodeDisplay { get; set; }
        public string CodeValue { get; set; }
        public List<CodeLink> Children  { get; set; }
        public string AttributeTags { get; set; }
        public string CodeValueFormat { get; set; }
        public bool isRoot { get; set; }
        public string args { get; set; }
        public void applyCodeValueFormat()
        {
            this.CodeValue = string.Format(CodeValueFormat, args.Split(','));
        }
    }
}
