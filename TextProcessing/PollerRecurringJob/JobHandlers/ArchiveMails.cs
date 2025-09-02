using Azure.Data.Tables;
using AzureServices;
using Dapper;
using EntityDto;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;
using ProjectKeys;
using RepositoryContract.ExternalReferenceGroup;
using RepositoryContract.Tickets;
using ServiceInterface;
using ServiceInterface.Storage;
using SqlTableRepository;

namespace PollerRecurringJob.JobHandlers
{
    internal static class ArchiveMails
    {
        internal static async Task Execute(PollerRecurringJob jobContext)
        {
            var tableStorageService = jobContext.provider.GetRequiredService<TableStorageService>();
            var externalRefRepository = jobContext.provider.GetRequiredService<IExternalReferenceGroupRepository>();
            var cacheManager = jobContext.provider.GetRequiredService<ICacheManager<TicketEntity>>();
            IWorkflowTrigger client = jobContext.provider.GetRequiredService<IWorkflowTrigger>();
            var _msgs = await client.GetWork<List<ArchiveMail>>("archivemail");

            foreach (var message in _msgs)
            {
                try
                {
                    if(!message.Model.Any()) await client.ClearWork("archivemail", [message]);
                    var sample = message.Model[0];
                    var minRKey = message.Model.Min(t => t.RowKey);
                    var maxRKey = message.Model.Max(t => t.RowKey);
                    string filter = TableClient.CreateQueryFilter(
                        $"PartitionKey eq {sample.PartitionKey} and RowKey ge {minRKey} and RowKey le {maxRKey}"
                    );
                    var items = tableStorageService.Query<TicketEntity>(filter, sample.FromTable);
                    var toUpsert = items.Where(t => message.Model.Any(x => x.RowKey == t.RowKey)).ToList();
                    if (toUpsert.Any())
                    {
                        await tableStorageService.PrepareUpsert(toUpsert).ExecuteBatch(sample.ToTable);

                        using (var connection = new SqlConnection(Environment.GetEnvironmentVariable(KeyCollection.ConnectionString)))
                        {
                            DynamicParameters dParam = new();
                            dParam.Add("@tableName", sample.ToTable);
                            dParam.Add("@eType", nameof(TicketEntity));
                            var sql = toUpsert.FromValues(dParam, "items", t => t.PartitionKey, t => t.RowKey);
                            await connection.ExecuteAsync(@$"
                                                    UPDATE ExternalReferenceGroup  
                                                    SET TableName = @tableName
                                                    FROM ExternalReferenceGroup ex
                                                    JOIN {sql} on ex.PartitionKey = items.PartitionKey and ex.RowKey = items.RowKey
                                                    WHERE ex.EntityType = @eType", dParam);
                        }

                        await tableStorageService.PrepareDelete(toUpsert).ExecuteBatch(sample.FromTable);
                    }
                    else
                    {
                        ActorEventSource.Current.ActorMessage(jobContext, $@"No Items To Upsert");
                    }
                }
                catch (Exception ex)
                {
                    ActorEventSource.Current.ActorMessage(jobContext, $@"Exception : {ex.Message}. {ex.StackTrace}");
                    await client.Trigger("archivemailpoison", message.Model);
                }
                finally
                {
                    await client.ClearWork("archivemail", [message]);
                    await cacheManager.Bust(nameof(TicketEntity), true, null);
                    await cacheManager.InvalidateOurs(nameof(TicketEntity));
                }
            }
        }
    }
}
