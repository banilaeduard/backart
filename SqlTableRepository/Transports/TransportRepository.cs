using Dapper;
using Microsoft.Data.SqlClient;
using ProjectKeys;
using RepositoryContract.ExternalReferenceGroup;
using RepositoryContract.Tickets;
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
                var multi = await connection.QueryMultipleAsync($@"
                                    {TransportSql.GetTransports()} WHERE ID = {transportId};
                                    {TransportSql.GetTransportItems(transportId)};
                                    {TransportSql.GetAttachmetns(transportId)};");

                var transport = multi.Read<TransportEntry>().First();
                transport.TransportItems = multi.Read<TransportItemEntry>().ToList();
                transport.ExternalReferenceEntries = multi.Read<ExternalReferenceGroupEntry>().ToList();

                return transport;
            }
        }

        public async Task<List<TransportEntry>> GetTransports(DateTime? since = null, int? pageSize = null)
        {
            using (var connection = GetConnection())
            {
                var sql = string.Empty;
                if (!pageSize.HasValue && !since.HasValue)
                {
                    sql = $@"{TransportSql.GetTransports()} WHERE [CurrentStatus] <> 'Delivered' ORDER BY Delivered DESC;

                            {TransportSql.GetTransports(20)} WHERE [CurrentStatus] = 'Delivered' ORDER BY Delivered DESC;";
                    var multi = await connection.QueryMultipleAsync(sql);
                    return [.. multi.Read<TransportEntry>(), .. multi.Read<TransportEntry>()];
                }
                else
                {
                    sql = $@"{TransportSql.GetTransports(pageSize!.Value)} WHERE [CurrentStatus] = 'Delivered' AND Delivered < @since ORDER BY Delivered DESC";
                    return [.. await connection.QueryAsync<TransportEntry>(sql)];
                }
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

                populateTransportItemsWithParentId(transportEntry.TransportItems, transport.Id);
                if (transportEntry.TransportItems?.Count > 0)
                {
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
                populateTransportItemsWithParentId(transportEntry.TransportItems ?? [], transport.Id);

                if (hasItems || hasRemove)
                {
                    var dParams = new DynamicParameters();
                    dParams.Add("detetedTransportItems", detetedTransportItems ?? []);
                    var fromSql = transportEntry.TransportItems!.FromValues(dParams, "transportItemValues",
                        t => t.ExternalItemId2,
                        t => t.ExternalItemId,
                        t => t.ItemId,
                        t => t.ItemName,
                        t => t.TransportId,
                        t => t.ExternalReferenceId,
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

        public async Task<List<ExternalReferenceGroupEntry>> HandleExternalAttachmentRefs(List<ExternalReferenceGroupEntry>? externalReferenceGroupEntries, int transportId, int[] deteledAttachments)
        {
            using (var connection = GetConnection())
            {
                if (externalReferenceGroupEntries?.Count > 0)
                {
                    for (int i = 0; i < externalReferenceGroupEntries.Count; i++)
                    {
                        externalReferenceGroupEntries[i].Id = transportId;
                        externalReferenceGroupEntries[i].TableName = "Transport";
                        externalReferenceGroupEntries[i].EntityType = nameof(AttachmentEntry);
                    }
                    var dParams = new DynamicParameters();
                    var fromSql = externalReferenceGroupEntries!.FromValues(dParams, "externalAttachments",
                        t => t.PartitionKey,
                        t => t.ExternalGroupId,
                        t => t.EntityType,
                        t => t.Id,
                        t => t.RowKey,
                        t => t.TableName);
                    await connection.ExecuteAsync($@"{TransportSql.InsertExternalAttachments(fromSql, "externalAttachments", transportId)};", dParams);
                }
                if (deteledAttachments?.Count() > 0)
                {
                    await connection.ExecuteAsync(TransportSql.EnsureAttachmentDeleted(transportId), new { deteledAttachments });
                }
                return [.. await connection.QueryAsync<ExternalReferenceGroupEntry>(TransportSql.GetAttachmetns(transportId))];
            }
        }

        private SqlConnection GetConnection() => new SqlConnection(Environment.GetEnvironmentVariable(KeyCollection.ConnectionString));
    }
}
