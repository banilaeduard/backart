using Azure.Storage.Files.Shares;
using Services.Storage;

namespace AzureServices
{
    public class AzureFileStorage : IStorageService
    {
        ShareClient share;
        public AzureFileStorage()
        {
            share = new(Environment.GetEnvironmentVariable("storage_connection"), "quer");
        }
        public byte[] Access(string fName, out string contentType)
        {
            var rootDirectory = share.GetRootDirectoryClient();
            var client = rootDirectory.GetFileClient(fName).Download();
            contentType = client.Value.ContentType;
            return BinaryData.FromStream(client.Value.Content).ToArray();
        }

        public byte[] AccessIfExists(string fName, out string contentType)
        {
            contentType = "";
            var rootDirectory = share.GetRootDirectoryClient();
            if (!rootDirectory.GetFileClient(fName).Exists()) return [];
            var client = rootDirectory.GetFileClient(fName).Download();
            contentType = client.Value.ContentType;
            return BinaryData.FromStream(client.Value.Content).ToArray();
        }

        public void Delete(string fName)
        {
            var rootDirectory = share.GetRootDirectoryClient();
            rootDirectory.GetFileClient(fName).DeleteIfExists();
        }

        public bool Exists(string fName)
        {
            var rootDirectory = share.GetRootDirectoryClient();
            return rootDirectory.GetFileClient(fName).Exists();
        }

        public void WriteTo(string fName, BinaryData file)
        {
            var rootDirectory = share.GetRootDirectoryClient();
            rootDirectory.GetFileClient(fName).Upload(file.ToStream());
        }
    }
}
