﻿using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
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

        public DateTime Check(string fName)
        {
            var blob = client.GetBlobClient(fName);
            BlobContentInfo bCon = null;

            if (!blob.Exists())
            {
                bCon = client.UploadBlob(fName, new BinaryData([])).Value;
                return bCon.LastModified.UtcDateTime;
            }
            return blob.GetProperties().Value.LastModified.UtcDateTime;
        }
        public void SetMetadata(string fName, Dictionary<string, string> metadata = null)
        {
            var blob = client.GetBlobClient(fName);
            BlobContentInfo bCon = null;

            if (blob.Exists())
            {
                blob.SetMetadata(metadata ?? new Dictionary<string, string>() { { "busted", DateTime.UtcNow.ToString() } });
            }
            else
            {
                client.UploadBlob(fName, new BinaryData([]));
            }
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
