using ClosedXML.Excel;

namespace WorkSheetServices
{
    public static class WorkbookReader
    {
        public static List<T> ReadWorkBook<T>(Stream stream, int firstDataRow = 4, string sheetName = "") where T : class, new()
        {
            return ReadWorkBook(stream, new Settings<T>()
            {
                FirstDataRow = firstDataRow
            }, sheetName);
        }

        public static List<T> ReadWorkBook<T>(Stream stream, Settings<T> settings, string sheetName = "") where T : class, new()
        {
            List<T> lst = new();

            using (var workbook = new XLWorkbook(stream))
            {
                var worksheet = string.IsNullOrEmpty(sheetName) ? workbook.Worksheets.FirstOrDefault()! : workbook.Worksheet(sheetName);
                var max_row_count = !settings.LastDataRow.HasValue ? worksheet.RowCount() : settings.LastDataRow;

                for (var i = settings.FirstDataRow; i < max_row_count; i++)
                {
                    if (!settings.LastDataRow.HasValue && worksheet.Cell(i, 1).IsEmpty()) break;

                    lst.Add(settings.Mapper.ReadLines((col, row) => worksheet.Cell(row, col).Value, i));
                }
            }

            return lst;
        }
    }
}