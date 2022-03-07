namespace Storage
{
    using Microsoft.Extensions.Logging;
    using System;
    using System.IO;
    using System.Security.Cryptography;
    using System.Text;
    using core;
    public class NetworkStorageService : IStorageService, IDisposable
    {
        private ILogger<NetworkStorageService> logger;
        private HashAlgorithm algorithm;
        public NetworkStorageService(ILogger<NetworkStorageService> logger)
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

        public string SaveBase64(string base64, string name)
        {
            return Save(Convert.FromBase64String(base64), name);
        }

        public string Save(byte[] content, string name)
        {
            var hash = ProbePath(content);
            DirectoryInfo dir = new DirectoryInfo(string.Format("{0}/{1}", StoragePath, hash.Substring(0, 2)));

            if (!dir.Exists) dir.Create();

            var file = new FileInfo(Path.Combine(
                                    dir.FullName,
                                    string.Format("{0}{1}", hash, Path.GetExtension(name)
                                    ))
                );

            if (!file.Exists) // you may not want to overwrite existing files
            {
                Stream stream = file.OpenWrite();
                stream.WriteAsync(content, 0, content.Length).Forget(logger, () =>
                      stream.DisposeAsync());
            }
            return file.FullName;
        }

        public Stream TryAquireStream(string pathTo, string fileName, out string filePath)
        {
            DirectoryInfo dir = new DirectoryInfo(Path.Combine(StoragePath, pathTo));
            if (!dir.Exists) dir.Create();

            var file = new FileInfo(Path.Combine(
                                    dir.FullName,
                                    fileName
                                    ));
            filePath = file.FullName;

            return file.OpenWrite();
        }
    }
}
