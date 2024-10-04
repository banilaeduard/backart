namespace Services.Storage
{
    public interface IStorageService
    {
        byte[] AccessIfExists(string fName, out string contentType);
        byte[] Access(string fName, out string contentType);
        void WriteTo(string fName, BinaryData file);
        void Delete(string fName);
        bool Exists(string fName);
    }
}