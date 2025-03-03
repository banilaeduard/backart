using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using RepositoryContract;
using RepositoryContract.Report;
using WebApi.Models;

namespace WebApi.Services
{
    public class ReclamatiiReport
    {
        private ConnectionSettings _settings;
        private IReportEntryRepository _reportsRepository;
        public ReclamatiiReport(ConnectionSettings settings, IReportEntryRepository reportsRepository)
        {
            _settings = settings;
            _reportsRepository = reportsRepository;
        }

        public async Task<byte[]> GenerateReport(ComplaintDocument complaintDocument)
        {
            var templateCustomPath = await _reportsRepository.GetReportTemplate(complaintDocument.LocationCode!, "Reclamatii");
            string templatePath = $@"{_settings.SqlQueryCache}/{templateCustomPath.TemplateName}";

            //System.IO.File.Copy(templatePath, outputPath, true);
            using (var ms = new MemoryStream())
            {
                using (var fs = File.Open(templatePath, FileMode.Open, FileAccess.Read)) { fs.CopyTo(ms); }
                ms.Position = 0;

                // Open the document
                using (WordprocessingDocument wordDoc = WordprocessingDocument.Open(ms, true))
                {
                    // Access the main document part
                    var mainPart = wordDoc.MainDocumentPart!;
                    var body = mainPart.Document.Body!;

                    // Replace placeholders in the paragraphs
                    ReplaceContentControlText(mainPart, "date_field", complaintDocument.Date.ToString("dd.MM.yyyy"));
                    ReplaceContentControlText(mainPart, "magazin_field", complaintDocument.LocationName);
                    Table table = body.Elements<Table>().FirstOrDefault()!;

                    for (int i = 0; i < complaintDocument.complaintEntries.Length; i++)
                    {
                        // Create a new row
                        TableRow newRow = new TableRow();
                        var complaint = complaintDocument.complaintEntries[i];
                        // Add cells to the new row
                        newRow.Append(CreateCell((i + 1).ToString()));
                        newRow.Append(CreateCell(complaint.Description));
                        newRow.Append(CreateCell(complaint.UM));
                        newRow.Append(CreateCell(complaint.Quantity));
                        newRow.Append(CreateCell(complaint.Observation));

                        // Insert the new row at the end of the table
                        table.Append(newRow);
                    }
                    mainPart.Document.Save();
                }
                ms.Position = 0;
                return ms.ToArray();
            }
        }

        static TableCell CreateCell(string text)
        {
            TableCell cell = new TableCell();
            cell.Append(new TableCellProperties(
                new TableCellWidth { Type = TableWidthUnitValues.Auto }));
            cell.Append(new Paragraph(new Run(new Text(text))));
            return cell;
        }

        static void ReplaceContentControlText(MainDocumentPart mainPart, string title, string newText)
        {
            foreach (var sdt in mainPart.Document.Descendants<SdtElement>())
            {
                if (sdt.InnerText.Trim().ToLower() == title)
                {
                    var textElement = sdt.Descendants<Text>().FirstOrDefault();
                    if (textElement != null)
                    {
                        textElement.Text = newText;
                    }
                }
            }
        }
    }
}