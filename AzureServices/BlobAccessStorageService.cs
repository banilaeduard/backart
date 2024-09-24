using Azure.Storage.Blobs;
using Services.Storage;

namespace AzureServices
{
    public class BlobAccessStorageService : IStorageService
    {
        BlobContainerClient client;
        public BlobAccessStorageService()
        {
            client = new(Environment.GetEnvironmentVariable("storage_connection"), "importstorage");
        }

        public byte[] Access(string fName, out string contentType)
        {
            var blob = client.GetBlobClient(fName).DownloadContent();

            contentType = blob.Value.Details.ContentType;
            return blob.Value.Content.ToArray();
        }

        public void Delete(string fName)
        {
            client.DeleteBlobIfExists(fName);
        }

        public void WriteTo(string fName, BinaryData file)
        {
            client.DeleteBlobIfExists(fName);
            client.UploadBlob(fName, file);
        }
    }
}
