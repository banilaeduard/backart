using Azure.Data.Tables;
using Microsoft.Extensions.Logging;
using System.Linq.Expressions;

namespace AzureServices
{
    public class TableStorageService
    {
        private ILogger<TableStorageService> logger;
        public TableStorageService(ILogger<TableStorageService> logger) { this.logger = logger; }

        public IQueryable<T> Query<T>(Expression<Func<T, bool>> expr, string? tableName = null) where T : class, ITableEntity
        {
            tableName = tableName ?? typeof(T).Name;
            TableClient tableClient = new(Environment.GetEnvironmentVariable("storage_connection"), tableName, new TableClientOptions());
            tableClient.CreateIfNotExists();
            return tableClient.Query(expr).AsQueryable();
        }

        public void Insert<T>(T entry, string? tableName = null) where T : class, ITableEntity
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

        public void Upsert(ITableEntity entry, string? tableName = null)
        {
            tableName = tableName ?? entry.GetType().Name;
            TableClient tableClient = new(Environment.GetEnvironmentVariable("storage_connection"), tableName, new TableClientOptions());
            tableClient.CreateIfNotExists();
            tableClient.UpsertEntity(entry);
        }

        public void Delete<T>(T entry, string? tableName = null) where T : class, ITableEntity
        {
            tableName = tableName ?? typeof(T).Name;
            TableClient tableClient = new(Environment.GetEnvironmentVariable("storage_connection"), tableName, new TableClientOptions());
            tableClient.CreateIfNotExists();
            tableClient.DeleteEntity(entry.PartitionKey, entry.RowKey);
        }

        public async Task ExecuteBatch(IEnumerable<TableTransactionAction> transactionActions, string? tableName = null)
        {
            if (!transactionActions.Any()) return;

            tableName = tableName ?? transactionActions.ElementAt(0).Entity.GetType().Name;
            TableClient tableClient = new(Environment.GetEnvironmentVariable("storage_connection"), tableName, new TableClientOptions());
            tableClient.CreateIfNotExists();
            foreach (var batch in Batch(transactionActions))
                await tableClient.SubmitTransactionAsync(batch).ConfigureAwait(false);
        }

        public (IEnumerable<TableTransactionAction> items, TableStorageService self) PrepareInsert<T>(IEnumerable<T> entries) where T : class, ITableEntity
        {
            return (entries.Select(e => new TableTransactionAction(TableTransactionActionType.Add, e)), this);
        }

        public (IEnumerable<TableTransactionAction> items, TableStorageService self) PrepareDelete<T>(IEnumerable<T> entries) where T : class, ITableEntity
        {
            return (entries.Select(e => new TableTransactionAction(TableTransactionActionType.Delete, e)), this);
        }

        public (IEnumerable<TableTransactionAction> items, TableStorageService self) PrepareUpsert<T>(IEnumerable<T> entries) where T : class, ITableEntity
        {
            return (entries.Select(e => new TableTransactionAction(TableTransactionActionType.UpsertReplace, e)), this);
        }

        private IEnumerable<List<TableTransactionAction>> Batch(IEnumerable<TableTransactionAction> items)
        {
            if (items.Count() == 0) yield return [];
            var skip = 0;
            var take = 99;
            foreach (var entityGroup in items.GroupBy(t => t.Entity.PartitionKey))
            {
                skip = 0;
                var count = entityGroup.Count() / take;
                for (int i = 0; i <= count; i++)
                {
                    yield return entityGroup.Skip(skip).Take(take).ToList();
                    skip += take;
                }
            }
        }
    }
}