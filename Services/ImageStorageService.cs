namespace WebApi.Services
{
    using Microsoft.Extensions.Logging;
    using System;
    using System.IO;
    using System.Security.Cryptography;
    using System.Text;

    public interface IStorageService
    {
        string StorageName { get; }
        string StoragePath { get; }
        string StorageType { get; }
        string Save(string path, string name);
        void Delete(string path);
    }
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

        public string Save(string path, string name)
        {
            algorithm.Initialize();

            StringBuilder sb = new StringBuilder();
            foreach (byte b in algorithm.ComputeHash(Encoding.UTF8.GetBytes(path)))
                sb.Append(b.ToString("X2"));
            sb.ToString();
            var hash = sb.ToString();
            Console.WriteLine(hash);

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
                byte[] _file = Convert.FromBase64String(path);
                stream.WriteAsync(_file, 0, _file.Length).Forget(logger, () =>
                    stream.DisposeAsync());
            }
            // return string.Format("{0}/{1}/{2}{3}", StoragePath, hash.Substring(0, 2), hash, Path.GetExtension(name));
            return file.FullName;
        }
    }
}