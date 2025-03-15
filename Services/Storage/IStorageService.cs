namespace ServiceInterface.Storage
{
    public interface IStorageService
    {
        byte[] AccessIfExists(string fName, out string contentType);
        byte[] Access(string fName, out string contentType);
        Task WriteTo(string fName, BinaryData file, bool replace = false);
        Task Delete(string fName);
        Task<bool> Exists(string fName);
    }
}