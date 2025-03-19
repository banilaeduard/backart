using AzureServices;
using Dapper;
using EntityDto.CommitedOrders;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using RepositoryContract;
using RepositoryContract.Orders;
using ServiceInterface.Storage;
using System.Text;

namespace SqlTableRepository.Orders
{
    public class OrdersRepositorySql : IOrdersRepository
    {
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
            throw new NotImplementedException();
        }

        public async Task<List<OrderEntry>> GetOrders(Func<OrderEntry, bool> expr, string? table = null)
        {
            using (var connection = new SqlConnection(ConnectionSettings.ExternalConnectionString))
            {
                return [.. (await connection.QueryAsync<OrderEntry>(TryAccess("orders.sql"), new { Date2 = DateTime.Now.AddMonths(-6) })).Where(expr)];
            }
        }

        public async Task<List<OrderEntry>> GetOrders(string? table = null)
        {
            using (var connection = new SqlConnection(ConnectionSettings.ExternalConnectionString))
            {
                return [.. await connection.QueryAsync<OrderEntry>(TryAccess("orders.sql"), new { Date2 = DateTime.Now.AddMonths(-6) })];
            }
        }

        public async Task ImportOrders(IList<Order> items, DateTime when)
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
                logger.LogInformation(new EventId(69), ex, $@"Accesing cloud for missing {key}");
                return Encoding.UTF8.GetString(storageService.Access(key, out var _));
            }
        }
    }
}
