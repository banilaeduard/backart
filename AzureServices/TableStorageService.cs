﻿using Azure.Data.Tables;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Linq.Expressions;

namespace AzureServices
{
    public class TableStorageService
    {
        private ILogger<TableStorageService> logger;
        public TableStorageService(ILogger<TableStorageService> logger) { this.logger = logger; }

        public Azure.Pageable<T> Query<T>(Expression<Func<T, bool>> expr, string? tableName = null) where T : class, ITableEntity
        {
            tableName = tableName ?? typeof(T).Name;
            TableClient tableClient = new(Environment.GetEnvironmentVariable("storage_connection"), tableName, new TableClientOptions());
            tableClient.CreateIfNotExists();
            return tableClient.Query(expr);
        }

        public async Task Insert<T>(T entry, string? tableName = null) where T : class, ITableEntity
        {
            tableName = tableName ?? entry.GetType().Name;
            TableClient tableClient = new(Environment.GetEnvironmentVariable("storage_connection"), tableName, new TableClientOptions());
            tableClient.CreateIfNotExists();
            await tableClient.AddEntityAsync(entry);
        }

        public async Task Upsert(ITableEntity entry, string? tableName = null)
        {
            tableName = tableName ?? entry.GetType().Name;
            TableClient tableClient = new(Environment.GetEnvironmentVariable("storage_connection"), tableName, new TableClientOptions());
            tableClient.CreateIfNotExists();
            await tableClient.UpsertEntityAsync(entry);
        }

        public async Task Update(ITableEntity entry, string? tableName = null)
        {
            tableName = tableName ?? entry.GetType().Name;
            TableClient tableClient = new(Environment.GetEnvironmentVariable("storage_connection"), tableName, new TableClientOptions());
            tableClient.CreateIfNotExists();
            await tableClient.UpdateEntityAsync(entry, entry.ETag);
        }

        public async Task Delete<T>(T entry, string? tableName = null) where T : class, ITableEntity
        {
            tableName = tableName ?? typeof(T).Name;
            TableClient tableClient = new(Environment.GetEnvironmentVariable("storage_connection"), tableName, new TableClientOptions());
            tableClient.CreateIfNotExists();
            await tableClient.DeleteEntityAsync(entry.PartitionKey, entry.RowKey);
        }

        public async Task Delete(string partitionKey, string rowKey, string tableName)
        {
            Debug.Assert(tableName != null);
            TableClient tableClient = new(Environment.GetEnvironmentVariable("storage_connection"), tableName, new TableClientOptions());
            tableClient.CreateIfNotExists();
            await tableClient.DeleteEntityAsync(partitionKey, rowKey);
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