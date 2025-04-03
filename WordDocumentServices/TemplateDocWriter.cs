using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using ServiceImplementation;
using WordDocument.Services;

namespace WordDocumentServices.Services
{
    public class TemplateDocWriter : ITemplateDocumentWriter
    {
        private bool closeStream = false;

        private Stream stream;
        private WordprocessingDocument doc;
        private Dictionary<string, Table> tableCache = new Dictionary<string, Table>();

        public TemplateDocWriter(Stream fStream)
        {
            stream = fStream;
            doc = WordprocessingDocument.Open(stream, true, new OpenSettings()
            {
                AutoSave = true,
            });
            closeStream = stream != Stream.Null;
        }

        public void Dispose()
        {
            tableCache.Clear();

            if (doc != null)
            {
                doc.Dispose();
            }

            if (closeStream)
            {
                stream?.Close();
            }
        }

        public ITemplateDocumentWriter SetTemplate(string templatePath)
        {
            return new TemplateDocWriter(TempFileHelper.CreateTempFile(templatePath));
        }

        public void WriteToMainDoc(Dictionary<string, string> keyValuePairs)
        {
            DocXServiceHelper.ReplaceContentControlText(doc.MainDocumentPart!, keyValuePairs);
        }

        public void WriteToTable(string tagName, string[][] values)
        {
            if (!tableCache.TryGetValue(tagName, out var table))
            {
                table = DocXServiceHelper.FindTableByTagOrDefault(doc.MainDocumentPart!, tagName);
                tableCache.Add(tagName, table);
            }
            foreach (var linevalues in values)
            {
                TableRow newRow = new TableRow();

                foreach (var val in linevalues)
                {
                    newRow.Append(DocXServiceHelper.CreateCell(val));
                }

                table.Append(newRow);
            }
        }

        public Stream GetStream()
        {
            doc.Dispose();
            doc = null;
            closeStream = false;
            if (stream.CanSeek)
                stream.Position = 0;
            return stream;
        }
    }
}
