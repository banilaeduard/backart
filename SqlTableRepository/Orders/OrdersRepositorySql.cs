using AzureServices;
using Dapper;
using EntityDto;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using RepositoryContract.Orders;
using Services.Storage;
using System.Collections.Concurrent;
using System.Text;

namespace SqlTableRepository.Orders
{
    public class OrdersRepositorySql : IOrdersRepository
    {
        static readonly string syncName = $"sync_control/LastSyncDate_${typeof(ComandaVanzareEntry).Name}";
        static readonly MemoryCache sqlCache = new(new MemoryCacheOptions() { ExpirationScanFrequency = TimeSpan.FromMinutes(10) });
        static readonly ConcurrentDictionary<string, string> prevValues = new ConcurrentDictionary<string, string>();
        
        private IStorageService storageService;
        private ILogger<OrdersRepositorySql> logger;

        public OrdersRepositorySql(IStorageService storageService, ILogger<OrdersRepositorySql> logger)
        {
            this.storageService = storageService;
            this.logger = logger;
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
            using (var connection = new SqlConnection(Environment.GetEnvironmentVariable("external_sql_server")))
            {
                return [.. (await connection.QueryAsync<ComandaVanzareEntry>(TryAccess("QImport/orders.txt"), new { Date2 = DateTime.Now.AddMonths(-6) })).Where(expr)];
            }
        }

        public async Task<List<ComandaVanzareEntry>> GetOrders(string? table = null)
        {
            using (var connection = new SqlConnection(Environment.GetEnvironmentVariable("external_sql_server")))
            {
                return [.. await connection.QueryAsync<ComandaVanzareEntry>(TryAccess("QImport/orders.txt"), new { Date2 = DateTime.Now.AddMonths(-6) })];
            }
        }

        public async Task ImportOrders(IList<ComandaVanzare> items, DateTime when)
        {
            throw new NotImplementedException();
        }

        private string TryAccess(string key)
        {
            if (!sqlCache.TryGetValue<string>(key, out var sql))
            {
                var s = "";
                try
                {
                    s = Encoding.UTF8.GetString(storageService.Access(key, out var contentType));
                    prevValues.AddOrUpdate(key, s, (x, y) => s);
                }
                catch (Exception ex)
                {
                    logger.LogWarning(new EventId(22), ex, "External Source Orders");
                    prevValues.TryGetValue(key, out s);
                }

                return sqlCache.Set(key, s, new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromMinutes(15)))!;
            }
            return sql!;
        }
    }
}
