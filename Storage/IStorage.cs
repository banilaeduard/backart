using System.IO;
using System.Threading.Tasks;

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
        void Delete(string path);
    }
}
