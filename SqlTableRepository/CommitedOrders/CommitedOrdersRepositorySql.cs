using AzureServices;
using Dapper;
using EntityDto;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using RepositoryContract.CommitedOrders;
using Services.Storage;
using SqlTableRepository.Orders;
using System.Collections.Concurrent;
using System.Text;

namespace SqlTableRepository.CommitedOrders
{
    public class CommitedOrdersRepositorySql : ICommitedOrdersRepository
    {
        static readonly string syncName = $"sync_control/LastSyncDate_${nameof(DispozitieLivrareEntry)}";
        static readonly MemoryCache sqlCache = new(new MemoryCacheOptions() { ExpirationScanFrequency = TimeSpan.FromMinutes(10) });
        static readonly ConcurrentDictionary<string, string> prevValues = new ConcurrentDictionary<string, string>();

        private IStorageService storageService;
        private ILogger<OrdersRepositorySql> logger;

        public CommitedOrdersRepositorySql(IStorageService storageService, ILogger<OrdersRepositorySql> logger)
        {
            this.storageService = storageService;
            this.logger = logger;
        }

        public Task DeleteCommitedOrders(List<DispozitieLivrareEntry> items)
        {
            throw new NotImplementedException();
        }

        public async Task<List<DispozitieLivrareEntry>> GetCommitedOrders(Func<DispozitieLivrareEntry, bool> expr)
        {
            using (var connection = new SqlConnection(Environment.GetEnvironmentVariable("external_sql_server")))
            {
                return [.. Aggregate((await connection.QueryAsync<DispozitieLivrareEntry>(TryAccess("QImport/disp.txt"), new { Date1 = DateTime.Now.AddDays(-12) })).Where(expr))];
            }
        }

        public async Task<List<DispozitieLivrareEntry>> GetCommitedOrders()
        {
            using (var connection = new SqlConnection(Environment.GetEnvironmentVariable("external_sql_server")))
            {
                return [.. Aggregate(await connection.QueryAsync<DispozitieLivrareEntry>(TryAccess("QImport/disp.txt"), new { Date1 = DateTime.Now.AddDays(-12) }))];
            }
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

        public Task ImportCommitedOrders(IList<DispozitieLivrare> items, DateTime when)
        {
            throw new NotImplementedException();
        }

        public Task InsertCommitedOrder(DispozitieLivrareEntry item)
        {
            throw new NotImplementedException();
        }

        public Task SetDelivered(int internalNumber)
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
                    if (!prevValues.ContainsKey(key))
                        prevValues.AddOrUpdate(key, s, (x, y) => s);
                }
                catch (Exception ex)
                {
                    logger.LogWarning(new EventId(22), ex, "External Source Commited");
                    prevValues.TryGetValue(key, out s);
                }

                return sqlCache.Set(key, s, new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromMinutes(15)))!;
            }
            prevValues.AddOrUpdate(key, sql!, (x, y) => sql!);
            return sql!;
        }

        private IEnumerable<DispozitieLivrareEntry> Aggregate(IEnumerable<DispozitieLivrareEntry> items)
        {
            foreach (var group in items.GroupBy(t => new { t.NumarIntern, t.CodProdus, t.CodLocatie, t.NumarComanda }))
                yield return DispozitieLivrareEntry.create(group.ElementAt(0), group.Sum(t => t.Cantitate));
        }
    }
}
