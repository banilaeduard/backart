using PdfiumViewer;
using SkiaSharp;

namespace ServiceImplementation
{
    public class PdfToSkiaImageService
    {
        public async IAsyncEnumerable<SKBitmap> RenderPdfPages(string pdfPath, int dpi = 300)
        {
            await using var fs = await GetStreamAsync(pdfPath);
            using var document = PdfDocument.Load(fs);
            for (int i = 0; i < document.PageCount; i++)
            {
                using var bmp = document.Render(i, dpi, dpi, true);
                await using var fStream = new MemoryStream();//TempFileHelper.CreateTempFile();

                bmp.Save(fStream, System.Drawing.Imaging.ImageFormat.Png);
                fStream.Seek(0, SeekOrigin.Begin);
                if (document.PageCount == i + 1) { fs.Close(); document.Dispose(); }
                yield return SKBitmap.Decode(fStream);
            }
        }

        private async Task<FileStream> GetStreamAsync(string pdfPath, int retryCount = 0, int maxCount = 3)
        {
            try
            {
                return new FileStream(pdfPath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096);
            }
            catch (Exception ex)
            {
                if (retryCount < maxCount)
                {
                    await Task.Delay((retryCount + 1) * 2000);
                    return await GetStreamAsync(pdfPath, ++retryCount, maxCount);
                }
                throw;
            }
        }
    }
}
