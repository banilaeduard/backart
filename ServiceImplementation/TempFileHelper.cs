namespace ServiceImplementation
{
    using System;
    using System.Threading.Tasks;

    public static class TempFileHelper
    {
        /// <summary>
        /// Creates a temporary file that is automatically deleted when disposed.
        /// </summary>
        /// <param name="content">Optional initial content (default is empty).</param>
        /// <returns>A FileStream wrapped in AutoDeletingTempFile.</returns>
        public static Stream CreateTempFile(string fromFile = null)
        {
            string tempFilePath = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".tmp");
            FileStream fileStream = null;

            // Write initial content if provided
            if (!string.IsNullOrEmpty(fromFile))
            {
                File.Copy(fromFile, tempFilePath, false);
                fileStream = new FileStream(tempFilePath, FileMode.Open, FileAccess.ReadWrite, FileShare.Read, 4096, FileOptions.DeleteOnClose);
                return fileStream;
            }
            // Create the temp file
            fileStream = new FileStream(tempFilePath, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.Read, 4096, FileOptions.DeleteOnClose);

            return fileStream;
        }

        public static async Task<Stream> CreateTempFile(byte[] byteArr, string fPath = null)
        {
            string tempFilePath = fPath ?? Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".tmp");
            var fileStream = new FileStream(tempFilePath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read, 4096, FileOptions.DeleteOnClose);
            fileStream.Write(byteArr, 0, byteArr.Count() - 1);
            return fileStream;
        }
    }

    /// <summary>
    /// A wrapper around FileStream that automatically deletes the file when disposed.
    /// </summary>
    public class AutoDeletingTempFile : IDisposable, IAsyncDisposable
    {
        public static AutoDeletingTempFile Null = new AutoDeletingTempFile("", (FileStream)FileStream.Null);
        public string FilePath { get; }
        private FileStream FileStream { get; }

        public AutoDeletingTempFile(string filePath, FileStream fileStream)
        {
            FilePath = filePath;
            FileStream = fileStream;
        }

        public FileStream GetStream() => FileStream;

        public void Dispose()
        {
            FileStream.Dispose();
            try
            {
                if (File.Exists(FilePath))
                {
                    File.Delete(FilePath);
                    Console.WriteLine($"Temporary file deleted: {FilePath}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to delete temporary file: {ex.Message}");
            }
        }

        public ValueTask DisposeAsync()
        {
            FileStream.DisposeAsync().GetAwaiter().GetResult();
            try
            {
                if (File.Exists(FilePath))
                {
                    File.Delete(FilePath);
                    Console.WriteLine($"Temporary file deleted: {FilePath}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to delete temporary file: {ex.Message}");
            }

            return ValueTask.CompletedTask;
        }
    }
}
