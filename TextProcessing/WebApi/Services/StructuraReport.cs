using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using RepositoryContract;
using RepositoryContract.CommitedOrders;
using RepositoryContract.ProductCodes;
using RepositoryContract.Report;
using ServiceImplementation;

namespace WebApi.Services
{
    public class StructuraReport
    {
        private ConnectionSettings _settings;
        private IProductCodeRepository _productCodeRepository;
        private IReportEntryRepository _reportsRepository;

        public StructuraReport(ConnectionSettings settings, IProductCodeRepository productCodeRepository, IReportEntryRepository reportsRepository)
        {
            _settings = settings;
            _productCodeRepository = productCodeRepository;
            _reportsRepository = reportsRepository;
        }

        public async Task<Stream> GenerateReport(List<CommitedOrderEntry> items, string reportName)
        {
            var reportsRows = await _reportsRepository.GetReportEntry(reportName);
            var reportCodes = await _productCodeRepository.GetProductCodes(c =>
                                            reportsRows.Any(r => c.Code.ToLowerInvariant().Contains(r.FindBy.ToLowerInvariant()) && c.Level == r.Level));

            var docSample = items.First();
            var templateCustomPath = await _reportsRepository.GetReportTemplate(docSample.CodLocatie!, reportName);
            string templatePath = Path.Combine(_settings.SqlQueryCache, templateCustomPath.TemplateName);

            var fStream = TempFileHelper.CreateTempFile(templatePath);

            // Open the document
            using (WordprocessingDocument wordDoc = WordprocessingDocument.Open(fStream, true))
            {
                // Access the main document part
                var mainPart = wordDoc.MainDocumentPart!;
                var body = mainPart.Document.Body!;
                // Replace placeholders in the paragraphs
                ReplaceContentControlText(mainPart, "date_field", docSample.DataAviz?.ToString("dd.MM.yyyy") ?? "................");
                ReplaceContentControlText(mainPart, "magazin_field", docSample.NumeLocatie!);
                ReplaceContentControlText(mainPart, "aviz_field", docSample.NumarAviz?.ToString() ?? ".......");

                Table table = body.Elements<Table>().FirstOrDefault()!;

                int currIndex = 0;
                var currGroup = reportsRows[0].Group;
                for (int i = 0; i < reportsRows.Count; i++)
                {
                    var reportRow = reportsRows[i];
                    var reportCodeList = reportCodes.Where(x => x.Code.ToLowerInvariant().Contains(reportRow.FindBy.ToLowerInvariant()) && x.Level == reportRow.Level);
                    if (!reportCodeList.Any()) continue;

                    var codes = items.Where(x => reportCodeList.Any(rc => rc.RootCode == x.CodProdus)).ToList();

                    if (codes.Any())
                    {
                        if (currGroup != reportRow.Group && currIndex > 0)
                        {
                            TableRow emptyRow = new TableRow();
                            emptyRow.Append(new TableRowProperties(new TableRowHeight { HeightType = HeightRuleValues.Exact, Val = 300 }));
                            emptyRow.Append(CreateCell(""));
                            emptyRow.Append(CreateCell(""));
                            emptyRow.Append(CreateCell(""));
                            emptyRow.Append(CreateCell(""));
                            table.Append(emptyRow);
                            currGroup = reportRow.Group;
                        }
                        else if (currIndex == 0)
                        {
                            currGroup = reportRow.Group;
                        }

                        // Create a new row
                        TableRow newRow = new TableRow();
                        newRow.Append(new TableRowProperties(new TableRowHeight { HeightType = HeightRuleValues.Exact, Val = 300 }));
                        // Add cells to the new row
                        newRow.Append(CreateCell((++currIndex).ToString()));
                        newRow.Append(CreateCell(reportRow.Display));
                        newRow.Append(CreateCell(reportRow.UM));
                        newRow.Append(CreateCell(codes.Sum(x => x.Cantitate).ToString()));

                        // Insert the new row at the end of the table
                        table.Append(newRow);
                    }
                }
                if (currIndex == 0) return Stream.Null;
                mainPart.Document.Save();
            }
            fStream.Position = 0;
            return fStream;
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
