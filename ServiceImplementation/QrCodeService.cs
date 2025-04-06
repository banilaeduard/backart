using SkiaSharp;
using ZXing;
using ZXing.Common;
using ZXing.QrCode.Internal;
using ZXing.SkiaSharp.Rendering;

public class QrCodeService
{
   public string? DecodeQrCode(Stream image)
    {
        using var skStream = new SKManagedStream(image, true);
        using var bitmap = SKBitmap.Decode(skStream);

        return DecodeQrCode(bitmap);
    }

    public string? DecodeQrCode(SKBitmap bitmap)
    {
        var reader = new BarcodeReaderGeneric
        {
            AutoRotate = false,
            Options = new DecodingOptions()
            {
                TryInverted = false,
                TryHarder = false,
            }
        };

        var result = reader.Decode(bitmap);
        return result?.Text;
    }

    public Stream GenerateQrCode(string text, ErrorCorrectionLevel lvl, int size = 100)
    {
        var opts = new EncodingOptions
        {
            Width = size,
            Height = size,
            Margin = 1,
        };
        opts.Hints[EncodeHintType.ERROR_CORRECTION] = lvl;

        var writer = new BarcodeWriter<SKBitmap>
        {
            Format = BarcodeFormat.QR_CODE,
            Options = opts,
            Renderer = new SKBitmapRenderer()
            {
                Background = SKColors.White,
                Foreground = SKColors.Black,
            }
        };

        using var bitmap = writer.Write(text);
        using var image = SKImage.FromBitmap(bitmap);
        var data = image.Encode(SKEncodedImageFormat.Png, 100);
        return data.AsStream(true);
    }
}
