﻿using Dapper;
using EntityDto.Transports;
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

        public async Task<List<TransportEntry>> GetTransports(int skip = 0, int take = 0)
        {
            using (var connection = GetConnection())
            {
                var sql = string.Empty;
                if (skip == 0 && take == 0)
                {
                    return [.. await connection.QueryAsync<TransportEntry>($@"{TransportSql.GetTransports()} WHERE [CurrentStatus] <> 'Delivered' ORDER BY Delivered DESC, Id DESC;")];
                }
                else
                {
                    sql = $@"{TransportSql.GetTransports()} WHERE [CurrentStatus] = 'Delivered' ORDER BY Delivered DESC, Id DESC OFFSET {skip} ROWS FETCH NEXT {take} ROWS ONLY";
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
                    var qMulti = await connection.QueryMultipleAsync($@"
                                {TransportSql.InsertMissingTransportItems(fromSql, "transportItemValues", true)};
                                {TransportSql.GetTransportItems(transport.Id)};
                                {TransportSql.UpdateDesc2(transport.Id)};", dParams);

                    var items = qMulti.Read<TransportItemEntry>().ToList();
                    transport = qMulti.Read<TransportEntry>().First();
                    transport.TransportItems = items;
                }

                return transport;
            }
        }

        public async Task<TransportEntry> UpdateTransport(TransportEntry transportEntry, int[] detetedTransportItems)
        {
            using (var connection = GetConnection())
            {
                var query = await connection.QueryMultipleAsync($@"{TransportSql.UpdateTransport(transportEntry.Id)}; {TransportSql.GetAttachmetns(transportEntry.Id)}", param: new
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
                var transport = query.Read<TransportEntry>().First();
                var ExternalReferenceEntries = query.Read<ExternalReferenceGroupEntry>().ToList();


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
                                {TransportSql.GetTransportItems(transport.Id)}
                                {TransportSql.UpdateDesc2(transport.Id)};";

                    var qMulti = await connection.QueryMultipleAsync(sql, dParams);

                    var items = qMulti.Read<TransportItemEntry>().ToList();
                    transport = qMulti.Read<TransportEntry>().First();
                    transport.TransportItems = items;
                }

                transport.ExternalReferenceEntries = ExternalReferenceEntries;
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
                    await connection.ExecuteAsync(TransportSql.EnsureAttachmentDeleted(transportId, deteledAttachments));
                }
                return [.. await connection.QueryAsync<ExternalReferenceGroupEntry>(TransportSql.GetAttachmetns(transportId))];
            }
        }

        private SqlConnection GetConnection() => new SqlConnection(Environment.GetEnvironmentVariable(KeyCollection.ConnectionString));
    }
}
