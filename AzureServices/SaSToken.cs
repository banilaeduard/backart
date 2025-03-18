using Azure.Data.Tables;
using Azure.Storage.Blobs;
using Azure.Storage.Sas;
using EntityDto.Reports;
using System.Text;

namespace AzureServices
{
    public class SaSToken
    {
        BlobContainerClient client;
        public SaSToken()
        {
            client = new(Environment.GetEnvironmentVariable("storage_connection"), "importstorage");
        }

        public List<(string key, string connection)> GenerateSaSTokens()
        {
            return [
                ("blob", EncodeTo64(client.GenerateSasUri(BlobContainerSasPermissions.Read | BlobContainerSasPermissions.Create, DateTimeOffset.Now.AddHours(1)).AbsoluteUri)),
                //("locationMapEntryToken", EncodeTo64(getTableSaS($@"{nameof(LocationMap)}Entry", 1)))
                ];
        }

        private string getTableSaS(string tableName, int hours)
        {
            return (new TableClient(Environment.GetEnvironmentVariable("storage_connection"), tableName, new TableClientOptions())
                .GenerateSasUri(Azure.Data.Tables.Sas.TableSasPermissions.Read, DateTimeOffset.UtcNow.AddHours(hours)).AbsoluteUri);
        }

        private string EncodeTo64(string input)
        {
            return Convert.ToBase64String(Encoding.ASCII.GetBytes(input));
        }
    }
}
