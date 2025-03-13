using Dapper;
using Microsoft.Data.SqlClient;
using RepositoryContract.Transport;

namespace SqlTableRepository.Transport
{
    public class TransportRepository : ITransportRepository
    {
        public async Task DeleteTransport(int transportId)
        {
            using (var connection = GetConnection())
            {
                var sql = TransportSql.DeleteTransport(transportId);
                await connection.ExecuteAsync(sql);
            }
        }

        public async Task<TransportEntry> GetTransport(int transportId)
        {
            using (var connection = GetConnection())
            {
                var multi = await connection.QueryMultipleAsync($@"{TransportSql.GetTransport(transportId)}; {TransportSql.GetTransportItems(transportId)}");

                var transport = multi.Read<TransportEntry>().First();
                transport.TransportItems = multi.Read<TransportItem>().ToList();

                return transport;
            }
        }

        public async Task<List<TransportEntry>> GetTransports()
        {
            using (var connection = GetConnection())
            {
                return [.. await connection.QueryAsync<TransportEntry>($@"SELECT * FROM [dbo].[Transport] ORDER BY Id DESC")];
            }
        }

        public async Task<TransportEntry> SaveTransport(TransportEntry transportEntry)
        {
            using (var connection = GetConnection())
            {
                var transport = await connection.QuerySingleAsync<TransportEntry>($@"{TransportSql.InsertTransport}", param: new
                {
                    transportEntry.CarPlateNumber,
                    transportEntry.DriverName,
                    transportEntry.Description,
                    transportEntry.FuelConsumption,
                    transportEntry.CurrentStatus,
                    transportEntry.Distance,
                    transportEntry.ExternalItemId,
                    transportEntry.Delivered,
                });

                if (transportEntry.TransportItems?.Count > 0)
                {
                    populateTransportItemsWithParentId(transportEntry.TransportItems, transport.Id);
                    var dParams = new DynamicParameters();
                    var fromSql = transportEntry.TransportItems.FromValues(dParams, "transportItemValues",
                        t => t.ExternalItemId2,
                        t => t.ExternalItemId,
                        t => t.ItemId,
                        t => t.ItemName,
                        t => t.TransportId,
                        t => t.DocumentType);
                    transport.TransportItems = [.. await connection.QueryAsync<TransportItem>($@"
                                {TransportSql.InsertMissingTransportItems(fromSql, "transportItemValues")};
                                {TransportSql.GetTransportItems(transport.Id)}", dParams)];
                }

                return transport;
            }
        }

        public async Task<TransportEntry> UpdateTransport(TransportEntry transportEntry)
        {
            using (var connection = GetConnection())
            {
                var transport = await connection.QuerySingleAsync<TransportEntry>($@"{TransportSql.UpdateTransport(transportEntry.Id)}", param: new
                {
                    transportEntry.CarPlateNumber,
                    transportEntry.DriverName,
                    transportEntry.Description,
                    transportEntry.FuelConsumption,
                    transportEntry.CurrentStatus,
                    transportEntry.Distance,
                    transportEntry.ExternalItemId,
                    transportEntry.Delivered,
                });

                if (transportEntry.TransportItems?.Count > 0)
                {
                    populateTransportItemsWithParentId(transportEntry.TransportItems, transport.Id);
                    var dParams = new DynamicParameters();
                    var fromSql = transportEntry.TransportItems.FromValues(dParams, "transportItemValues",
                        t => t.ExternalItemId2,
                        t => t.ExternalItemId,
                        t => t.ItemId,
                        t => t.ItemName,
                        t => t.TransportId,
                        t => t.DocumentType);
                    var sql = $@"{TransportSql.UpdateTransportItems(fromSql, "transportItemValues")}; 
                                {TransportSql.InsertMissingTransportItems(fromSql, "transportItemValues")};
                                {TransportSql.GetTransportItems(transport.Id)}";

                    transport.TransportItems = [.. await connection.QueryAsync<TransportItem>(sql, dParams)];
                }

                return transport;
            }
        }

        private void populateTransportItemsWithParentId(List<TransportItem> tItems, int transportId)
        {
            for (int i = 0; i < tItems.Count; i++)
            {
                tItems[i].TransportId = transportId;
            }
        }

        private SqlConnection GetConnection() => new SqlConnection(Environment.GetEnvironmentVariable("ConnectionString"));
    }
}
