namespace DataAccess.Entities
{
    public class CodeLinkNode
    {
        public int ParentNode { get; set; }
        public virtual CodeLink Parent { get; set; }
        public int ChildNode { get; set; }
        public virtual CodeLink Child { get; set; }
    }
}
