namespace EntityDto
{
    [AttributeUsage(AttributeTargets.Property)]
    public class MapExcelAttribute : Attribute
    {
        int colNumber;
        public Type type;

        public MapExcelAttribute(int colNumber, Type srcType = null)
        {
            this.colNumber = colNumber;
            this.type = srcType;
        }

        public int GetColNumber() => colNumber;
        public Type GetParseFrom() => type;
    }
}
