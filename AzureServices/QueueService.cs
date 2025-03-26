using Azure.Storage.Queues;
using Newtonsoft.Json;
using ProjectKeys;
using ServiceInterface.Storage;

namespace AzureServices
{
    public class QueueService : IWorkflowTrigger
    {
        public static async Task<QueueClient> GetClient(string queueName)
        {
            var client = new QueueClient(Environment.GetEnvironmentVariable(KeyCollection.StorageConnection)!, queueName);
            await client.CreateIfNotExistsAsync();
            return client;
        }

        private static string Serialize<T>(T message)
        {
            return JsonConvert.SerializeObject(message, Formatting.Indented);
        }

        private static T? Deserialize<T>(string message)
        {
            return JsonConvert.DeserializeObject<T>(message);
        }

        public async Task Trigger<T>(string workflowName, T model) where T : class, new()
        {
            var client = await GetClient(workflowName);
            await client.SendMessageAsync(Serialize(model));
        }

        public async Task<List<WorkflowEntrityId<T>>> GetWork<T>(string workflowName) where T : class, new()
        {
            var client = await GetClient(workflowName);
            var messages = await client.ReceiveMessagesAsync(maxMessages: 32);
            var result = new List<WorkflowEntrityId<T>>(32);
            foreach (var msg in messages.Value.ToList())
            {
                if (msg.Body.Length > 0)
                    result.Add(new WorkflowEntrityId<T>(msg.MessageId, msg.PopReceipt, Deserialize<T>(msg.Body.ToString()), msg.InsertedOn?.UtcDateTime));
            }

            return result;
        }

        public async Task ClearWork<T>(string workflowName, WorkflowEntrityId<T>[] workflowIds)
        {
            var client = await GetClient(workflowName);
            foreach (var msg in workflowIds)
            {
                await client.DeleteMessageAsync(msg.Id, msg.Id2);
            }
        }
    }
}
