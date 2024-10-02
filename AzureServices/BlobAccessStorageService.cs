using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs.Specialized;
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

        public void SetMetadata(string fName, string leaseId, IDictionary<string, string> metadata = null)
        {
            var blob = client.GetBlobClient(fName);
            BlobContentInfo bCon = null;

            if (blob.Exists())
            {
                blob.SetMetadata(metadata ?? new Dictionary<string, string>() { { "busted", DateTime.UtcNow.ToString() } }
                , string.IsNullOrEmpty(leaseId) ? null : new BlobRequestConditions()
                {
                    LeaseId = leaseId
                });
            }
            else
            {
                client.UploadBlob(fName, new BinaryData([]));
            }
        }

        public BlobLeaseClient GetLease(string fName)
        {
            return client.GetBlobClient(fName).GetBlobLeaseClient();
        }

        public IDictionary<string, string> GetMetadata(string fName)
        {
            var blob = client.GetBlobClient(fName);
            if (blob.Exists())
            {
                return blob.GetProperties().Value.Metadata;
            }

            return new Dictionary<string, string>();
        }
    }
}
