using Dapper;
using Microsoft.Data.SqlClient;
using ProjectKeys;
using RepositoryContract.ExternalReferenceGroup;

namespace SqlTableRepository.ExternalReferenceGroup
{
    public class ExternalReferenceGroupSql : IExternalReferenceGroupRepository
    {
        public async Task DeleteExternalRefs(int[] externalRefs)
        {
            using (var connection = new SqlConnection(Environment.GetEnvironmentVariable(KeyCollection.ConnectionString)))
            {
                await connection.ExecuteAsync(ExternalRefSql.DeleteExternalRef, new { externalRefs });
            }
        }

        public async Task<List<ExternalReferenceGroupEntry>> GetExternalReferences(string whereClause = null)
        {
            using (var connection = new SqlConnection(Environment.GetEnvironmentVariable(KeyCollection.ConnectionString)))
            {
                return [.. await connection.QueryAsync<ExternalReferenceGroupEntry>(@$"SELECT * FROM ExternalReferenceGroup {(string.IsNullOrWhiteSpace(whereClause) ? "" : ("WHERE " + whereClause))};")];
            }
        }

        public async Task<List<ExternalReferenceGroupEntry>> UpsertExternalReferences(List<ExternalReferenceGroupEntry> externals)
        {
            DynamicParameters dParams = new();
            string fromName = "fParams";
            using (var connection = new SqlConnection(Environment.GetEnvironmentVariable(KeyCollection.ConnectionString)))
            {
                var fromSql = externals.FromValues(dParams, fromName, t => t.PartitionKey, t => t.RowKey, t => t.ExternalGroupId, t => t.TableName, t => t.EntityType, t => t.Id);
                return [.. await connection.QueryAsync<ExternalReferenceGroupEntry>(ExternalRefSql.UpsertExternalRef(fromSql, fromName), dParams)];
            }
        }
    }
}
