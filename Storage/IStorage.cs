using System.IO;

namespace Storage
{
    public interface IStorageService
    {
        string StorageName { get; }
        string StoragePath { get; }
        string StorageType { get; }
        string ProbePath(byte[] content);
        string SaveBase64(string base64, string name);
        string Save(byte[] stream, string name);
        Stream TryAquireStream(string pathTo, string fileName, out string filePath);
        void Delete(string path);
    }
}
