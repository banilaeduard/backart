namespace EntityDto
{
    [AttributeUsage(AttributeTargets.Property)]
    public class MapExcelAttribute : Attribute
    {
        int colNumber;
        int? rowNumber;
        public Type type;

        public MapExcelAttribute(int colNumber, int rowNumber = -1, Type srcType = null)
        {
            this.colNumber = colNumber;
            this.type = srcType;
            this.rowNumber = rowNumber;
        }

        public int GetColNumber() => colNumber;

        public int? GetRowNumber() => rowNumber > -1 ? rowNumber : null;
        public Type GetParseFrom() => type;
    }
}
