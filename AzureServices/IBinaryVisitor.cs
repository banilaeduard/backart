namespace AzureServices
{
    public interface IBinaryVisitor
    {
        public void Accept(BinaryWriter w);
        public void Accept(BinaryReader w);
    }
}
