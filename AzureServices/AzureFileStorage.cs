using Azure;
using Azure.Storage.Files.Shares;
using Azure.Storage.Files.Shares.Models;
using Azure.Storage.Files.Shares.Specialized;
using ProjectKeys;
using ServiceInterface.Storage;

namespace AzureServices
{
    public class AzureFileStorage : IStorageService, IMetadataService
    {
        ShareClient share;

        public AzureFileStorage()
        {
            share = new(Environment.GetEnvironmentVariable(KeyCollection.StorageConnection), Environment.GetEnvironmentVariable(KeyCollection.FileShareName));
        }
        public Stream Access(string fName, out string contentType)
        {
            GetParts(fName, out var parentDirectory, out var fileName);

            var client = parentDirectory.GetFileClient(fileName).Download();
            contentType = client.Value.ContentType;
            return client.Value.Content;
        }

        public bool AccessIfExists(string fName, out string contentType, out Stream content)
        {
            contentType = "";
            content = Stream.Null;

            GetParts(fName, out var parentDirectory, out var fileName);

            if (!parentDirectory.GetFileClient(fileName).Exists())
                return false;
            var client = parentDirectory.GetFileClient(fileName).Download();
            contentType = client.Value.ContentType;
            content = client.Value.Content;
            return true;
        }

        public async Task Delete(string fName)
        {
            GetParts(fName, out var parentDirectory, out var fileName);

            await parentDirectory.GetFileClient(fileName).DeleteAsync();
        }

        public async Task<bool> Exists(string fName)
        {
            GetParts(fName, out var rootDirectory, out var fileName);
            return await rootDirectory.GetFileClient(fileName).ExistsAsync();
        }

        public async Task WriteTo(string fName, Stream file, bool replace = false)
        {
            GetParts(fName, out var parentDirectory, out var fileName);

            var fileClient = parentDirectory.GetFileClient(fileName);

            await fileClient.CreateAsync(file.Length);
            await fileClient.UploadRangeAsync(new HttpRange(0, file.Length), file);

            await fileClient.SetHttpHeadersAsync(new ShareFileSetHttpHeadersOptions
            {
                HttpHeaders =
                new ShareFileHttpHeaders()
                {
                    CacheControl = "max-age=31536000"
                }
            });
        }

        public async Task SetMetadata(string fName, string? leaseId, IDictionary<string, string> metadata = null, params string[] args)
        {
            GetParts(fName, out var parentDirectory, out var fileName);
            var fileClient = parentDirectory.GetFileClient(fileName);
            if (fileClient.Exists())
            {
                await fileClient.SetMetadataAsync(metadata ?? new Dictionary<string, string>() { { "busted", DateTime.UtcNow.ToString() } }
                , string.IsNullOrEmpty(leaseId) ? null : new ShareFileRequestConditions()
                {
                    LeaseId = leaseId
                });
            }
            else
            {
                await fileClient.UploadAsync(new BinaryData([]).ToStream());
                await SetMetadata(args != null ? string.Format(fName, args) : fName, leaseId, metadata);
            }
        }
        public async Task<IDictionary<string, string>> GetMetadata(string fName, params string[] args)
        {
            GetParts(fName, out var parentDirectory, out var fileName);
            var fileClient = parentDirectory.GetFileClient(fileName);
            if (await fileClient.ExistsAsync())
            {
                return (await fileClient.GetPropertiesAsync()).Value.Metadata;
            }

            return new Dictionary<string, string>();
        }
        public async Task<ILeaseClient> GetLease(string fName, params string[] args)
        {
            GetParts(fName, out var parentDirectory, out var fileName);
            var fileClient = parentDirectory.GetFileClient(fileName);
            if (await fileClient.ExistsAsync())
            {
                return new FileShareClientInternal(fileClient.GetShareLeaseClient());
            }
            else
            {
                await fileClient.UploadAsync(new BinaryData([]).ToStream());
                return new FileShareClientInternal(fileClient.GetShareLeaseClient());
            }
        }

        private void GetParts(string fName, out ShareDirectoryClient directoryPath, out string fileName)
        {
            directoryPath = share.GetDirectoryClient(Path.GetDirectoryName(fName) ?? "");
            directoryPath.CreateIfNotExists();
            fileName = Path.GetFileName(fName);
        }

        public Task DeleteMetadata(string fName, params string[] args)
        {
            throw new NotImplementedException();
        }
    }

    class FileShareClientInternal : ILeaseClient, IDisposable, IAsyncDisposable
    {
        private ShareLeaseClient internalClient;
        public FileShareClientInternal(ShareLeaseClient internalClient)
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

        public ValueTask DisposeAsync()
        {
            internalClient.ReleaseAsync().GetAwaiter().GetResult();
            internalClient = null;
            return ValueTask.CompletedTask;
        }

        public async Task Release()
        {
            await internalClient.ReleaseAsync();
        }
    }
}
