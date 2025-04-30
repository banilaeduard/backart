using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Drawing.Wordprocessing;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace WordDocument.Services
{
    public static class DocXServiceHelper
    {
        public static async Task<WordprocessingDocument> CreateEmptyDoc(Stream stream)
        {
            var wordDoc = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document, true);
            var part = wordDoc.AddMainDocumentPart();
            part.Document = new Document();
            part.Document.Append(new Body());
            var destStylesPart = part.AddNewPart<StyleDefinitionsPart>();
            destStylesPart.Styles = new Styles();
            destStylesPart.Styles.Save();
            return wordDoc!;
        }

        public static async Task CloneDocument(Stream stream, MainDocumentPart target, int duplicates, Func<string, string> GetMd5, bool skipFirstpageBreak = false)
        {
            int dupes = duplicates;
            using var docToClone = WordprocessingDocument.Open(stream, false);
            while (dupes > 0)
            {
                if (skipFirstpageBreak || duplicates > dupes) AddPageBreak(target);
                CloneBody(docToClone.MainDocumentPart!, target, GetMd5);
                dupes--;
            }
        }

        public static void CloneBody(MainDocumentPart src, MainDocumentPart target, Func<string, string> GetMd5)
        {
            Dictionary<string, string> relMapping = new();
            var itemsToClone = src.Document.Body!.ChildElements.Select(d => d.CloneNode(true)).ToList();

            foreach (var image in src.GetPartsOfType<ImagePart>())
            {
                string srcMd5 = string.Empty;
                using (var reader = new StreamReader(image.GetStream()))
                {
                    srcMd5 = GetMd5(reader.ReadToEnd());
                }
                var srcId = src.GetIdOfPart(image);
                target.TryGetPartById(srcId, out var targetImage);

                if (targetImage != null)
                {
                    var targetMd5 = targetImage.Annotations<string>().FirstOrDefault();
                    if (targetMd5 != srcMd5)
                    {
                        var img = target.AddPart(image);
                        img.AddAnnotation(srcMd5);
                        relMapping[srcId] = target.GetIdOfPart(img);
                    }
                }
                else
                {
                    var img = target.AddPart(image, srcId);
                    img.AddAnnotation(srcMd5);
                }
            }

            foreach (var item in itemsToClone)
            {
                target.Document.Body!.AppendChild(item);
                if (relMapping.Any())
                {
                    if (item is DocumentFormat.OpenXml.Drawing.Pictures.Picture elementT)
                    {
                        UpdateImageReferences([elementT], relMapping);
                    }
                    else
                    {
                        UpdateImageReferences(item.Descendants<DocumentFormat.OpenXml.Drawing.Pictures.Picture>(), relMapping);
                    }
                }
            }

            var targetStyles = target.StyleDefinitionsPart.Styles.Elements<Style>().ToList();
            foreach (var style in src.StyleDefinitionsPart.Styles.Elements<Style>())
            {
                var srcStyle = targetStyles.FirstOrDefault(t => t.StyleId == style.StyleId);
                if (srcStyle == null)
                {
                    target.StyleDefinitionsPart.Styles.Append(style.CloneNode(true));
                    target.StyleDefinitionsPart.Styles.Save();
                }
            }
        }

        public static void UpdateImageReferences(IEnumerable<DocumentFormat.OpenXml.Drawing.Pictures.Picture> pictures, IDictionary<string, string> relationShipMapping)
        {
            foreach (var picture in pictures)
            {
                var blip = picture.Descendants<DocumentFormat.OpenXml.Drawing.Blip>().FirstOrDefault();
                if (blip != null)
                {
                    // Get the relationship ID of the image
                    var relId = blip.Embed;

                    if (relId != null && relationShipMapping.ContainsKey(relId))
                    {
                        // Update the relationship ID to the new one
                        blip.Embed = relationShipMapping[relId];
                    }
                }
            }
        }

        public static void AddPageBreak(MainDocumentPart mainPart)
        {
            if (mainPart == null)
                return;

            Body body = mainPart.Document.Body;

            // Create a new paragraph with a page break
            Paragraph paragraph = new Paragraph();
            Run run = new Run();
            Break pageBreak = new Break() { Type = BreakValues.Page };

            run.Append(pageBreak);
            paragraph.Append(run);

            // Append to the document body
            body.Append(paragraph);
        }

        public static void ReplaceContentControlText(MainDocumentPart mainPart, Dictionary<string, string> kvp)
        {
            foreach (var sdt in mainPart.Document.Descendants<SdtElement>())
            {
                if (kvp.ContainsKey(sdt.InnerText.Trim().ToLower()))
                {
                    var textElement = sdt.Descendants<Text>().FirstOrDefault();
                    if (textElement != null)
                    {
                        textElement.Text = kvp[sdt.InnerText.Trim().ToLower()];
                    }
                }
            }
        }

        public static void AddImagePart(MainDocumentPart mainPart, Stream imageStream, string placeholderTag, int length = 100, int width = 100, Func<string, string> GetMd5 = null)
        {
            var sdt = mainPart.Document.Body.Descendants<SdtElement>()
                .FirstOrDefault(s => s.SdtProperties.GetFirstChild<Tag>()?.Val == placeholderTag);
            if (sdt == null)
                return;

            ImagePart imagePart = null;
            if (GetMd5 != null)
            {
                using (var strReader = new StreamReader(imageStream, leaveOpen: true))
                {
                    imagePart = mainPart.AddImagePart(ImagePartType.Jpeg, $@"rId{GetMd5(strReader.ReadToEnd()).Substring(0, 6)}");
                    imageStream.Seek(0, SeekOrigin.Begin);
                }
            }
            else imagePart = mainPart.AddImagePart(ImagePartType.Jpeg);
            imagePart.FeedData(imageStream);

            var element = DocXDrawingService.CreateFloatingImage(mainPart.GetIdOfPart(imagePart), Guid.NewGuid().ToString(), length, width);

            sdt.RemoveAllChildren();
            sdt.AppendChild(new Inline(new Run(element)));
        }

        public static Table FindTableByTagOrDefault(MainDocumentPart documentPart, string tag)
        {
            var tables = documentPart.Document.Body.Elements<Table>();

            foreach (var table in tables)
            {
                var tableProperties = table.Elements<TableProperties>().FirstOrDefault();
                var description = tableProperties?.Elements<TableDescription>().FirstOrDefault();

                if (description != null && description.Val == tag)
                {
                    return table; // Found the table with the matching tag
                }
            }
            return tables.FirstOrDefault(); // No matching table found
        }

        public static TableCell CreateCell(string text)
        {
            TableCell cell = new TableCell();
            cell.Append(new TableCellProperties(
                new TableCellWidth { Type = TableWidthUnitValues.Auto }));
            cell.Append(new Paragraph(new Run(new Text(text))));
            return cell;
        }
    }
}
