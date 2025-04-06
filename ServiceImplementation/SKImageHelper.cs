using SkiaSharp;

namespace ServiceImplementation
{
    public static class SKImageHelper
    {
        private static readonly Random _random = new();

        public static void SaveImage(SKBitmap bitmap, string filePath, SKEncodedImageFormat format = SKEncodedImageFormat.Png, int quality = 100)
        {
            // Create an SKImage from the SKBitmap
            using var image = SKImage.FromBitmap(bitmap);

            // Encode the image in the desired format (e.g., PNG or JPEG)
            using var data = image.Encode(format, quality);

            // Save the encoded data to a file
            File.WriteAllBytes(filePath, data.ToArray());
        }
        public static SKBitmap RotateImage(SKBitmap originalImage, float angle)
        {
            // Create a new SKBitmap with the same size as the original
            SKBitmap rotatedImage = new SKBitmap(originalImage.Width, originalImage.Height);

            // Create a canvas to draw on the rotated image
            using (var canvas = new SKCanvas(rotatedImage))
            {
                // Clear the canvas with a transparent background (optional)
                canvas.Clear(SKColors.Transparent);

                // Set the pivot point (center of the image) for the rotation
                canvas.Translate(originalImage.Width / 2, originalImage.Height / 2);

                // Apply the rotation
                canvas.RotateDegrees(angle);

                // Draw the original image on the canvas at the center (after rotation)
                canvas.DrawBitmap(originalImage, new SKPoint(-originalImage.Width / 2, -originalImage.Height / 2));
            }

            return rotatedImage;
        }
        public static byte[] DegradeImage(byte[] inputImage, int targetWidth = 600, int targetHeight = 600, int jpegQuality = 60)
        {
            using var inputStream = new MemoryStream(inputImage);
            using var original = SKBitmap.Decode(inputStream);

            // Resize to simulate lower resolution
            using var resized = original.Resize(new SKImageInfo(targetWidth, targetHeight), SKFilterQuality.Medium);

            // Optional: Apply light blur to simulate scanner softness
            using var surface = SKSurface.Create(new SKImageInfo(targetWidth, targetHeight));
            var canvas = surface.Canvas;
            canvas.Clear(SKColors.White);

            var paint = new SKPaint
            {
                ImageFilter = SKImageFilter.CreateBlur(0.8f, 0.8f), // subtle blur
                FilterQuality = SKFilterQuality.Low
            };

            canvas.DrawBitmap(resized, 0, 0, paint);

            // Encode as compressed JPEG (low quality)
            using var image = surface.Snapshot();
            using var data = image.Encode(SKEncodedImageFormat.Jpeg, jpegQuality);

            return data.ToArray();
        }

        public static byte[] DamageImage(byte[] inputImage, int numberOfPatches = 5, int maxPatchSize = 20)
        {
            using var inputStream = new MemoryStream(inputImage);
            using var bitmap = SKBitmap.Decode(inputStream);

            using var surface = SKSurface.Create(new SKImageInfo(bitmap.Width, bitmap.Height));
            var canvas = surface.Canvas;

            // Draw original image
            canvas.DrawBitmap(bitmap, 0, 0);

            // Simulate damage: draw random white boxes
            var paint = new SKPaint
            {
                Color = SKColors.White,
                Style = SKPaintStyle.Fill
            };

            for (int i = 0; i < numberOfPatches; i++)
            {
                int patchWidth = _random.Next(5, maxPatchSize);
                int patchHeight = _random.Next(5, maxPatchSize);
                int x = _random.Next(0, bitmap.Width - patchWidth);
                int y = _random.Next(0, bitmap.Height - patchHeight);

                canvas.DrawRect(new SKRect(x, y, x + patchWidth, y + patchHeight), paint);
            }

            using var image = surface.Snapshot();
            using var data = image.Encode(SKEncodedImageFormat.Png, 100); // keep format PNG to avoid JPEG noise
            return data.ToArray();
        }
    }
}
