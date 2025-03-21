using Dapper;
using EntityDto.CommitedOrders;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using RepositoryContract;
using RepositoryContract.CommitedOrders;
using ServiceInterface.Storage;
using SqlTableRepository.Orders;

namespace SqlTableRepository.CommitedOrders
{
    public class CommitedOrdersRepositorySql<T> : ICommitedOrdersRepository where T : IStorageService
    {
        private IStorageService storageService;
        private ILogger<OrdersRepositorySql> logger;
        private ConnectionSettings ConnectionSettings;

        public CommitedOrdersRepositorySql(T storageService, ILogger<OrdersRepositorySql> logger, ConnectionSettings ConnectionSettings)
        {
            this.storageService = storageService;
            this.logger = logger;
            this.ConnectionSettings = ConnectionSettings;
        }

        public Task DeleteCommitedOrders(List<CommitedOrderEntry> items)
        {
            throw new NotImplementedException();
        }

        public async Task<List<CommitedOrderEntry>> GetCommitedOrders(int[] ids)
        {
            using (var connection = new SqlConnection(ConnectionSettings.ExternalConnectionString))
            {
                var sql = TryAccess("dispIds.sql");
                return [.. Aggregate(await connection.QueryAsync<CommitedOrderEntry>(sql, new { ids }))];
            }
        }

        public async Task<List<CommitedOrderEntry>> GetCommitedOrder(int id)
        {
            using (var connection = new SqlConnection(ConnectionSettings.ExternalConnectionString))
            {
                var sql = "[dbo].[CommitedOrderId]";
                return [.. Aggregate(await connection.QueryAsync<CommitedOrderEntry>(sql, new { NumarIntern = id }, commandType: System.Data.CommandType.StoredProcedure))];
            }
        }

        public async Task<List<CommitedOrderEntry>> GetCommitedOrders(DateTime? from)
        {
            using (var connection = new SqlConnection(ConnectionSettings.ExternalConnectionString))
            {
                var sql = "[dbo].[CommitedOrders]";
                return [.. Aggregate(await connection.QueryAsync<CommitedOrderEntry>(sql, new { @Date1 = from ?? DateTime.Now.AddMonths(-2) }, commandType: System.Data.CommandType.StoredProcedure))];
            }
        }

        public async Task<DateTime?> GetLastSyncDate()
        {
            throw new NotImplementedException();
        }

        public Task ImportCommitedOrders(IList<CommitedOrder> items, DateTime when)
        {
            throw new NotImplementedException();
        }

        public Task InsertCommitedOrder(CommitedOrderEntry item)
        {
            throw new NotImplementedException();
        }

        public async Task SetDelivered(int internalNumber, int? numarAviz)
        {
        }

        private string TryAccess(string key)
        {
            try
            {
                return File.ReadAllText(Path.Combine(ConnectionSettings.SqlQueryCache, key));
            }
            catch (Exception ex)
            {
                logger.LogInformation(new EventId(69), ex, @$"Accessing cloud for missing {key}");
                using (var stream = storageService.Access(key, out var _))
                using (var reader = new StreamReader(stream))
                    return reader.ReadToEnd();

            }
        }

        private IEnumerable<CommitedOrderEntry> Aggregate(IEnumerable<CommitedOrderEntry> items)
        {
            foreach (var group in items.GroupBy(t => new { t.NumarIntern, t.CodProdus, t.CodLocatie, t.NumarComanda }))
                yield return CommitedOrderEntry.create(group.ElementAt(0), group.Sum(t => t.Cantitate), group.Sum(x => x.Greutate ?? 0) * group.Sum(t => t.Cantitate));
        }
    }
}
