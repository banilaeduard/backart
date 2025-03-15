using Azure.Storage.Blobs.Models;
using Azure.Storage.Files.Shares;
using ServiceInterface.Storage;

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

        public async Task Delete(string fName)
        {
            var rootDirectory = share.GetRootDirectoryClient();
            await rootDirectory.GetFileClient(fName).DeleteAsync();
        }

        public async Task<bool> Exists(string fName)
        {
            var rootDirectory = share.GetRootDirectoryClient();
            return await rootDirectory.GetFileClient(fName).ExistsAsync();
        }

        public async Task WriteTo(string fName, BinaryData file, bool replace = false)
        {
            var rootDirectory = share.GetRootDirectoryClient();
            var fClient = rootDirectory.GetFileClient(fName);
            var exists = await fClient.ExistsAsync();
            if (!replace && exists) return;
            if (exists)
            {
                await fClient.DeleteAsync();
            }
            await fClient.UploadAsync(file.ToStream());
            await fClient.SetHttpHeadersAsync(new Azure.Storage.Files.Shares.Models.ShareFileSetHttpHeadersOptions
            {
                HttpHeaders =
                new Azure.Storage.Files.Shares.Models.ShareFileHttpHeaders()
                {
                    CacheControl = "max-age=31536000"
                }
            });
        }
    }
}
