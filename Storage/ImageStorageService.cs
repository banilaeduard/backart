namespace Storage
{
    using Microsoft.Extensions.Logging;
    using System;
    using System.IO;
    using System.Security.Cryptography;
    using System.Text;
    using core;
    public class ImageStorageService : IStorageService, IDisposable
    {
        private ILogger<ImageStorageService> logger;
        private HashAlgorithm algorithm;
        public ImageStorageService(ILogger<ImageStorageService> logger)
        {
            this.logger = logger;
            algorithm = SHA256.Create();
        }
        public string StorageName => "Images";

        public string StoragePath => "/photos";

        public string StorageType => "local";

        public void Delete(string path)
        {
            FileInfo file = new FileInfo(path);
            if (file.Exists) file.Delete();
        }

        public void Dispose()
        {
            algorithm.Dispose();
        }

        public string ProbePath(byte[] content)
        {
            algorithm.Initialize();
            StringBuilder sb = new StringBuilder();
            foreach (byte b in algorithm.ComputeHash(content))
                sb.Append(b.ToString("X2"));
            return sb.ToString();
        }

        public string Save(string path, string name)
        {
            var hash = ProbePath(Encoding.UTF8.GetBytes(path));
            DirectoryInfo dir = new DirectoryInfo(string.Format("{0}/{1}", StoragePath, hash.Substring(0, 2)));

            if (!dir.Exists) dir.Create();

            var file = new FileInfo(Path.Combine(
                                    dir.FullName,
                                    string.Format("{0}{1}", hash, Path.GetExtension(name)
                                    ))
                );

            if (!file.Exists) // you may not want to overwrite existing files
            {
                using (Stream stream = file.OpenWrite())
                {
                    byte[] _file = Convert.FromBase64String(path);
                    stream.WriteAsync(_file, 0, _file.Length).Forget(logger, () =>
                        stream.DisposeAsync());
                }
            }
            return file.FullName;
        }
    }
}