using Dapper;
using Microsoft.Data.SqlClient;
using RepositoryContract.Transports;

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
                var multi = await connection.QueryMultipleAsync($@"{TransportSql.GetTransports} WHERE ID = {transportId}; {TransportSql.GetTransportItems(transportId)}");

                var transport = multi.Read<TransportEntry>().First();
                transport.TransportItems = multi.Read<TransportItemEntry>().ToList();

                return transport;
            }
        }

        public async Task<List<TransportEntry>> GetTransports()
        {
            using (var connection = GetConnection())
            {
                return [.. await connection.QueryAsync<TransportEntry>($@"{TransportSql.GetTransports} ORDER BY Id DESC")];
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
                    transport.TransportItems = [.. await connection.QueryAsync<TransportItemEntry>($@"
                                {TransportSql.InsertMissingTransportItems(fromSql, "transportItemValues", true)};
                                {TransportSql.GetTransportItems(transport.Id)}", dParams)];
                }

                return transport;
            }
        }

        public async Task<TransportEntry> UpdateTransport(TransportEntry transportEntry, int[] detetedTransportItems)
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

                bool hasItems = transportEntry.TransportItems?.Count > 0;
                bool hasRemove = detetedTransportItems.Count() > 0;
                if (hasItems || hasRemove)
                {
                    populateTransportItemsWithParentId(transportEntry.TransportItems ?? [], transport.Id);
                    var dParams = new DynamicParameters();
                    dParams.Add("detetedTransportItems", detetedTransportItems ?? []);
                    var fromSql = transportEntry.TransportItems!.FromValues(dParams, "transportItemValues",
                        t => t.ExternalItemId2,
                        t => t.ExternalItemId,
                        t => t.ItemId,
                        t => t.ItemName,
                        t => t.TransportId,
                        t => t.DocumentType);
                    var sql = $@"{TransportSql.DeleteTransportItems(transport.Id, hasRemove)}
                                {TransportSql.UpdateTransportItems(fromSql, "transportItemValues", hasItems)}
                                {TransportSql.InsertMissingTransportItems(fromSql, "transportItemValues", hasItems)}
                                {TransportSql.GetTransportItems(transport.Id)}";

                    transport.TransportItems = [.. await connection.QueryAsync<TransportItemEntry>(sql, dParams)];
                }

                return transport;
            }
        }

        private void populateTransportItemsWithParentId(List<TransportItemEntry> tItems, int transportId)
        {
            for (int i = 0; i < tItems.Count; i++)
            {
                tItems[i].TransportId = transportId;
            }
        }

        private SqlConnection GetConnection() => new SqlConnection(Environment.GetEnvironmentVariable("ConnectionString"));
    }
}
