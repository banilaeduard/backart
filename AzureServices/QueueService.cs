using Azure.Storage.Queues;
using Newtonsoft.Json;

namespace AzureServices
{
    public static class QueueService
    {
        public static async Task<QueueClient> GetClient(string queueName)
        {
            var client = new QueueClient(Environment.GetEnvironmentVariable("storage_connection")!, queueName);
            await client.CreateIfNotExistsAsync();
            return client;
        }

        public static string Serialize<T>(T message)
        {
            return JsonConvert.SerializeObject(message, Formatting.Indented);
        }

        public static T? Deserialize<T>(string message)
        {
            return JsonConvert.DeserializeObject<T>(message);
        }
    }
}
