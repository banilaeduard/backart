using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace WebApi.Services
{
    public static class DocXServiceHelper
    {
        public static void CloneBody(MainDocumentPart src, MainDocumentPart target, Func<string,string> GetMd5)
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
    }
}
