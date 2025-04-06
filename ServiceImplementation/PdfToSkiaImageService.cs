using PdfiumViewer;
using SkiaSharp;

namespace ServiceImplementation
{
    public class PdfToSkiaImageService
    {
        public async IAsyncEnumerable<SKBitmap> RenderPdfPages(string pdfPath, int dpi = 300)
        {
            await using (var fs = new FileStream(pdfPath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096))
            {
                using var document = PdfDocument.Load(fs);
                for (int i = 0; i < document.PageCount; i++)
                {
                    using var bmp = document.Render(i, dpi, dpi, true);
                    await using var fStream = new MemoryStream();//TempFileHelper.CreateTempFile();

                    bmp.Save(fStream, System.Drawing.Imaging.ImageFormat.Png);
                    fStream.Seek(0, SeekOrigin.Begin);
                    if (document.PageCount == i + 1) document.Dispose();
                    yield return SKBitmap.Decode(fStream);
                }
            }
        }
    }
}
