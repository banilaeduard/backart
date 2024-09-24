using Azure.Data.Tables;
using Microsoft.Extensions.Logging;
using System.Linq.Expressions;

namespace AzureServices
{
    public class TableStorageService
    {
        private ILogger<TableStorageService> logger;
        public TableStorageService(ILogger<TableStorageService> logger) { this.logger = logger; }

        public void Insert(ITableEntity entry, string? tableName = null)
        {
            tableName = tableName ?? entry.GetType().Name;
            TableClient tableClient = new(Environment.GetEnvironmentVariable("storage_connection"), tableName, new TableClientOptions());
            tableClient.CreateIfNotExists();
            try
            {
                tableClient.AddEntity(entry);
            }
            catch (Exception ex)
            {
                logger.LogError(new EventId(tableName.GetHashCode(), tableName) { }, ex, "Entry: {0} - {1}", entry.PartitionKey, entry.RowKey);
            }
        }

        public List<T> Query<T>(Expression<Func<T, bool>> expr, string? tableName = null) where T : class, ITableEntity
        {
            tableName = tableName ?? typeof(T).Name;
            TableClient tableClient = new(Environment.GetEnvironmentVariable("storage_connection"), tableName, new TableClientOptions());
            return tableClient.Query(expr).ToList();
        }

        public void Delete<T>(List<T> entries, string? tableName = null) where T : class, ITableEntity
        {
            tableName = tableName ?? typeof(T).Name;
            TableClient tableClient = new(Environment.GetEnvironmentVariable("storage_connection"), tableName, new TableClientOptions());
            foreach (var entry in entries)
                tableClient.DeleteEntity(entry.PartitionKey, entry.RowKey);
        }
    }
}
