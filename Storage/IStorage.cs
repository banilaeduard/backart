using System.Threading.Tasks;

namespace Storage
{
    public interface IStorageService
    {
        string StorageName { get; }
        string StoragePath { get; }
        string StorageType { get; }
        string ProbePath(byte[] content);
        string Save(string path, string name);
        void Delete(string path);
    }
}
