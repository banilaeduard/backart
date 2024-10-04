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
            if (client.GetBlobClient(fName).Exists()) return;
            client.UploadBlob(fName, file);
        }

        public bool Exists(string fName)
        {
            return client.GetBlobClient(fName).Exists();
        }

        public void SetMetadata(string fName, string? leaseId, IDictionary<string, string> metadata = null, params string[] args)
        {
            var blob = client.GetBlobClient(args != null ? string.Format(fName, args) : fName);

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
                client.UploadBlob(args != null ? string.Format(fName, args) : fName, new BinaryData([]));
                SetMetadata(args != null ? string.Format(fName, args) : fName, leaseId, metadata);
            }
        }

        public BlobLeaseClient GetLease(string fName, params string[] args)
        {
            var blob = client.GetBlobClient(args != null ? string.Format(fName, args) : fName);
            if (blob.Exists())
            {
                return blob.GetBlobLeaseClient();
            }
            else
            {
                client.UploadBlob(args != null ? string.Format(fName, args) : fName, new BinaryData([]));
                return client.GetBlobClient(args != null ? string.Format(fName, args) : fName).GetBlobLeaseClient();
            }
        }

        public IDictionary<string, string> GetMetadata(string fName, params string[] args)
        {
            var blob = client.GetBlobClient(args != null ? string.Format(fName,args) : fName);
            if (blob.Exists())
            {
                return blob.GetProperties().Value.Metadata;
            }

            return new Dictionary<string, string>();
        }

        public byte[] AccessIfExists(string fName, out string contentType)
        {
            contentType = "";
            var blob = client.GetBlobClient(fName);

            if (!blob.Exists()) return [];

            var content = blob.DownloadContent();
            contentType = content.Value.Details.ContentType;
            return content.Value.Content.ToArray();
        }
    }
}
