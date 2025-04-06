using PdfiumViewer;
using SkiaSharp;

namespace ServiceImplementation
{
    public class PdfToSkiaImageService
    {
        public async IAsyncEnumerable<SKBitmap> RenderPdfPages(string pdfPath, int dpi = 300)
        {
            using var document = PdfDocument.Load(pdfPath);

            for (int i = 0; i < document.PageCount; i++)
            {
                using var bmp = document.Render(i, dpi, dpi, true);
                await using var fStream = TempFileHelper.CreateTempFile();

                bmp.Save(fStream, System.Drawing.Imaging.ImageFormat.Png);
                fStream.Seek(0, SeekOrigin.Begin);

                yield return SKBitmap.Decode(fStream);
            }
        }
    }
}
