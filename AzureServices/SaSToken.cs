using Azure.Storage.Blobs;

namespace AzureServices
{
    public class SaSToken
    {
        BlobContainerClient client;
        public SaSToken()
        {
            client = new(Environment.GetEnvironmentVariable("storage_connection"), "importstorage");
        }

        public string GenerateSaSToken()
        {
            //Azure.Storage.Sas.BlobSasBuilder blobSasBuilder = new Azure.Storage.Sas.BlobSasBuilder()
            //{
            //    BlobContainerName = "demo-copy",
            //    BlobName = "test.txt",
            //    ExpiresOn = DateTime.UtcNow.AddDays(5),
            //};
            //blobSasBuilder.SetPermissions(Azure.Storage.Sas.BlobSasPermissions.Read);//User will only be able to read the blob and it's properties
            //var sasToken = blobSasBuilder.ToSasQueryParameters(new StorageSharedKeyCredential(accountName, accountKey)).ToString();
            //var sasUrl = blobClient.Uri.AbsoluteUri + "?" + sasToken;
            return client.GenerateSasUri(Azure.Storage.Sas.BlobContainerSasPermissions.Read, DateTimeOffset.Now.AddDays(5)).AbsoluteUri;
        }
    }
}
