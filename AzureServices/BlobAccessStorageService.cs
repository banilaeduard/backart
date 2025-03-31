using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs.Specialized;
using ProjectKeys;
using ServiceInterface.Storage;

namespace AzureServices
{
    public class BlobAccessStorageService : IStorageService, IMetadataService
    {
        BlobContainerClient client;
        public BlobAccessStorageService()
        {
            client = new(Environment.GetEnvironmentVariable(KeyCollection.StorageConnection), Environment.GetEnvironmentVariable(KeyCollection.BlobShareName));
        }

        public bool AccessIfExists(string fName, out string contentType, out Stream content)
        {
            contentType = "";
            content = Stream.Null;

            var blob = client.GetBlobClient(fName);
            if (!blob.Exists()) return false;

            var file = blob.DownloadContent();
            contentType = file.Value.Details.ContentType;
            content = file.Value.Content.ToStream();
            return true;
        }

        public Stream Access(string fName, out string contentType)
        {
            var blob = client.GetBlobClient(fName).DownloadContent();
            contentType = blob.Value.Details.ContentType;
            return blob.Value.Content.ToStream();
        }

        public async Task Delete(string fName)
        {
            await client.DeleteBlobIfExistsAsync(fName);
        }

        public async Task WriteTo(string fName, Stream file, bool replace = false)
        {
            var exists = await client.GetBlobClient(fName).ExistsAsync();
            if (!replace && exists) return;
            if (exists)
            {
                await client.DeleteBlobAsync(fName);
            }
            await client.UploadBlobAsync(fName, file);
            await client.GetBlobClient(fName).SetHttpHeadersAsync(new BlobHttpHeaders { CacheControl = "max-age=31536000" });
        }

        public async Task<bool> Exists(string fName)
        {
            return await client.GetBlobClient(fName).ExistsAsync();
        }

        public async Task SetMetadata(string fName, string? leaseId, IDictionary<string, string> metadata = null, params string[] args)
        {
            var blob = client.GetBlobClient(args != null ? string.Format(fName, args) : fName);

            if (blob.Exists())
            {
                await blob.SetMetadataAsync(metadata ?? new Dictionary<string, string>() { { "busted", DateTime.UtcNow.ToString() } }
                , string.IsNullOrEmpty(leaseId) ? null : new BlobRequestConditions()
                {
                    LeaseId = leaseId
                });
            }
            else
            {
                client.UploadBlob(args != null ? string.Format(fName, args) : fName, new BinaryData([]));
                await SetMetadata(args != null ? string.Format(fName, args) : fName, leaseId, metadata);
            }
        }
        public async Task<IDictionary<string, string>> GetMetadata(string fName, params string[] args)
        {
            var blob = client.GetBlobClient(args != null ? string.Format(fName, args) : fName);
            if (await blob.ExistsAsync())
            {
                return (await blob.GetPropertiesAsync()).Value.Metadata;
            }

            return new Dictionary<string, string>();
        }
        public async Task<ILeaseClient> GetLease(string fName, params string[] args)
        {
            var blob = client.GetBlobClient(args != null ? string.Format(fName, args) : fName);
            if (await blob.ExistsAsync())
            {
                return new BlobLeaseClientInternal(blob.GetBlobLeaseClient());
            }
            else
            {
                client.UploadBlob(args != null ? string.Format(fName, args) : fName, new BinaryData([]));
                return new BlobLeaseClientInternal(client.GetBlobClient(args != null ? string.Format(fName, args) : fName).GetBlobLeaseClient());
            }
        }

        public Task DeleteMetadata(string fName, params string[] args)
        {
            throw new NotImplementedException();
        }
    }

    class BlobLeaseClientInternal : ILeaseClient, IDisposable
    {
        private BlobLeaseClient internalClient;
        public BlobLeaseClientInternal(BlobLeaseClient internalClient)
        {
            this.internalClient = internalClient;
        }
        public string LeaseId => internalClient.LeaseId;

        public async Task<ILeaseClient> Acquire(TimeSpan time)
        {
            await internalClient.AcquireAsync(time);
            return this;
        }

        public void Dispose()
        {
            internalClient.Release();
            internalClient = null;
        }

        public async Task Release()
        {
            await internalClient.ReleaseAsync();
        }
    }
}
