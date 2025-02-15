﻿using AzureServices;
using Dapper;
using EntityDto;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using RepositoryContract;
using RepositoryContract.Orders;
using Services.Storage;
using System.Text;

namespace SqlTableRepository.Orders
{
    public class OrdersRepositorySql : IOrdersRepository
    {
        static readonly string syncName = $"sync_control/LastSyncDate_${typeof(ComandaVanzareEntry).Name}";

        private IStorageService storageService;
        private ILogger<OrdersRepositorySql> logger;
        ConnectionSettings ConnectionSettings;

        public OrdersRepositorySql(AzureFileStorage storageService, ILogger<OrdersRepositorySql> logger, ConnectionSettings ConnectionSettings)
        {
            this.storageService = storageService;
            this.logger = logger;
            this.ConnectionSettings = ConnectionSettings;
        }

        public async Task<DateTime?> GetLastSyncDate()
        {
            var blobAccessStorageService = new BlobAccessStorageService();
            var metadata = blobAccessStorageService.GetMetadata(syncName);

            if (metadata.ContainsKey("data_sync"))
            {
                return DateTime.Parse(metadata["data_sync"]);
            }
            return null;
        }

        public async Task<List<ComandaVanzareEntry>> GetOrders(Func<ComandaVanzareEntry, bool> expr, string? table = null)
        {
            using (var connection = new SqlConnection(ConnectionSettings.ExternalConnectionString))
            {
                return [.. (await connection.QueryAsync<ComandaVanzareEntry>(TryAccess("orders.sql"), new { Date2 = DateTime.Now.AddMonths(-6) })).Where(expr)];
            }
        }

        public async Task<List<ComandaVanzareEntry>> GetOrders(string? table = null)
        {
            using (var connection = new SqlConnection(ConnectionSettings.ExternalConnectionString))
            {
                return [.. await connection.QueryAsync<ComandaVanzareEntry>(TryAccess("orders.sql"), new { Date2 = DateTime.Now.AddMonths(-6) })];
            }
        }

        public async Task ImportOrders(IList<ComandaVanzare> items, DateTime when)
        {
            throw new NotImplementedException();
        }

        private string TryAccess(string key)
        {
            try
            {
                return File.ReadAllText(Path.Combine(ConnectionSettings.SqlQueryCache, key));
            }
            catch (Exception ex)
            {
                return Encoding.UTF8.GetString(storageService.Access(key, out var _));
            }
        }
    }
}
