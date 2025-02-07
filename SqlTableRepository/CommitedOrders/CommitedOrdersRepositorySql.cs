using AzureServices;
using Dapper;
using EntityDto;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using RepositoryContract;
using RepositoryContract.CommitedOrders;
using Services.Storage;
using SqlTableRepository.Orders;
using System.Text;

namespace SqlTableRepository.CommitedOrders
{
    public class CommitedOrdersRepositorySql : ICommitedOrdersRepository
    {
        static readonly string syncName = $"sync_control/LastSyncDate_${nameof(DispozitieLivrareEntry)}";

        private IStorageService storageService;
        private ILogger<OrdersRepositorySql> logger;
        private ConnectionSettings ConnectionSettings;

        public CommitedOrdersRepositorySql(AzureFileStorage storageService, ILogger<OrdersRepositorySql> logger, ConnectionSettings ConnectionSettings)
        {
            this.storageService = storageService;
            this.logger = logger;
            this.ConnectionSettings = ConnectionSettings;
        }

        public Task DeleteCommitedOrders(List<DispozitieLivrareEntry> items)
        {
            throw new NotImplementedException();
        }

        public async Task<List<DispozitieLivrareEntry>> GetCommitedOrders(Func<DispozitieLivrareEntry, bool> expr)
        {
            using (var connection = new SqlConnection(ConnectionSettings.ExternalConnectionString))
            {
                return [.. Aggregate((await connection.QueryAsync<DispozitieLivrareEntry>(TryAccess("disp.sql"), new { Date1 = DateTime.Now.AddMonths(-2) })).Where(expr))];
            }
        }

        public async Task<List<DispozitieLivrareEntry>> GetCommitedOrder(int id)
        {
            using (var connection = new SqlConnection(ConnectionSettings.ExternalConnectionString))
            {
                return [.. Aggregate(await connection.QueryAsync<DispozitieLivrareEntry>(TryAccess("dispOrder.sql"), new { NumarIntern = id }))];
            }
        }

        public async Task<List<DispozitieLivrareEntry>> GetCommitedOrders()
        {
            using (var connection = new SqlConnection(ConnectionSettings.ExternalConnectionString))
            {
                return [.. Aggregate(await connection.QueryAsync<DispozitieLivrareEntry>(TryAccess("disp.sql"), new { Date1 = DateTime.Now.AddMonths(-2) }))];
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

        public async Task SetDelivered(int[] internalNumbers)
        {
            //using (var connection = new SqlConnection(Environment.GetEnvironmentVariable("ConnectionString")))
            //{
            //}
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

        private IEnumerable<DispozitieLivrareEntry> Aggregate(IEnumerable<DispozitieLivrareEntry> items)
        {
            foreach (var group in items.GroupBy(t => new { t.NumarIntern, t.CodProdus, t.CodLocatie, t.NumarComanda }))
                yield return DispozitieLivrareEntry.create(group.ElementAt(0), group.Sum(t => t.Cantitate), group.Sum(x => x.Greutate ?? 0) * group.Sum(t => t.Cantitate));
        }
    }
}
