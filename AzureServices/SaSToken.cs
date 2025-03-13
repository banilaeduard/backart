using Azure.Data.Tables;
using Azure.Storage;
using Azure.Storage.Blobs;
using Azure.Storage.Sas;

namespace AzureServices
{
    public class SaSToken
    {
        BlobContainerClient client;
        TableClient tableClient;
        public SaSToken()
        {
            client = new(Environment.GetEnvironmentVariable("storage_connection"), "importstorage");
            tableClient = new(Environment.GetEnvironmentVariable("storage_connection"), "LocationMap" , new TableClientOptions());
        }

        public (string,string) GenerateSaSToken()
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

            return (client.GenerateSasUri(BlobContainerSasPermissions.Read | BlobContainerSasPermissions.Create, DateTimeOffset.Now.AddHours(15)).AbsoluteUri, 
                tableClient.GenerateSasUri(Azure.Data.Tables.Sas.TableSasPermissions.Read, DateTimeOffset.Now.AddHours(15)).AbsoluteUri);
        }

        // the server side


        public static IEnumerable<(Uri BlobUri, string SharedAccessSignature)> serverAPIgetUploadAddresses(string blobName)
        {
            var storageAccounts = new[] {
                new {
                    FrontDoorName = "xxxxx",
                    ShardName = "$root",
                    AccountName = "xxxxxx",
                    AccountKey = "xxxxx",
                    ContainerName = "importstorage"
                }
            };
            var locations = storageAccounts.Select(x =>
            {
                Uri blobUri = new($"https://{x.FrontDoorName}/{x.ShardName}/{blobName}");

                var sas = new BlobSasBuilder(
                    permissions: BlobSasPermissions.Write | BlobSasPermissions.Create,
                    expiresOn: DateTimeOffset.UtcNow.AddHours(1))
                {
                    BlobContainerName = x.ContainerName,
                    BlobName = blobName,
                    Resource = "b",
                }.ToSasQueryParameters(sharedKeyCredential: new StorageSharedKeyCredential(
                        accountName: x.AccountName,
                        accountKey: x.AccountKey))
                    .ToString();

                return (blobUri, sas);
            }).ToArray();
            // Here we derive a shard key from the user ID.
            //
            // So some users will get the sequence
            // [ 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 ], while others get
            // [ 3, 4, 5, 6, 7, 8, 9, 0, 1, 2 ] or
            // [ 4, 5, 6, 7, 8, 9, 0, 1, 2, 3 ].
            //
            // This way we can distribute the load across all storage accounts.
            //
            //var startShard = userId.GetHashCode() % storageAccounts.Length;
            return locations.Skip(0).Concat(locations.Take(1));
        }
    }
}
