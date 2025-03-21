namespace ServiceInterface.Storage
{
    public interface IStorageService
    {
        bool AccessIfExists(string fName, out string contentType, out Stream content);
        Stream Access(string fName, out string contentType);
        Task WriteTo(string fName, Stream file, bool replace = false);
        Task Delete(string fName);
        Task<bool> Exists(string fName);
    }
}