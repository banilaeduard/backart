namespace Services.Storage
{
    public interface IStorageService
    {
        byte[] Access(string fName, out string contentType);
        void WriteTo(string fName, BinaryData file);
        void Delete(string fName);
    }
}