namespace SqlTableRepository
{
    internal static class SqlDateTimeHelper
    {
        internal static readonly DateTime SqlDateTimeMin = new DateTime(1753, 1, 1, 0, 0, 0);
        internal static readonly DateTime SqlDateTimeMax = new DateTime(9999, 12, 31, 23, 59, 59, 997); // SQL datetime max precision

        internal static DateTime ClampToSqlDateTimeRange(DateTime dateTime)
        {
            if (dateTime < SqlDateTimeMin) return SqlDateTimeMin;
            if (dateTime > SqlDateTimeMax) return SqlDateTimeMax;
            return dateTime;
        }
    }
}
