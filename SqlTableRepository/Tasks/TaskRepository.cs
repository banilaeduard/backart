using Dapper;
using Microsoft.Data.SqlClient;
using RepositoryContract.Tasks;
using RepositoryContract.Tickets;

namespace SqlTableRepository.Tasks
{
    public class TaskRepository : ITaskRepository
    {
        public async Task<TaskEntry> InsertFromTicketEntries(TicketEntity[] tickets)
        {
            DynamicParameters dParam = new();

            var fromSql = tickets.FromValues(dParam, "tickets", t => t.PartitionKey, t => t.RowKey);
            TaskEntry taskEntry = null;
            TaskAction taskAction = null;
            var count = tickets.Count();

            using (var connection = new SqlConnection(Environment.GetEnvironmentVariable("ConnectionString")))
            {
                taskEntry = await connection.QuerySingleAsync<TaskEntry>($"" +
                                      $"INSERT INTO dbo.TaskEntry(Name, Details) " +
                                      $"OUTPUT INSERTED.*" +
                                      $"VALUES(@Name, @Details);"
                                      , new { Name = tickets[0].LocationCode, Details = tickets[0].Description });

                taskAction = await connection.QuerySingleAsync<TaskAction>($"" +
                                      $"INSERT INTO dbo.TaskAction([TaskId],[ActionId],[Description])" +
                                      $"OUTPUT INSERTED.*" +
                                      $"VALUES(@TaskId, 1, 'Created from tickets[' + @Count + ']');"
                                      , new { TaskId = taskEntry.Id, Count = count.ToString() });

                var sql = $"INSERT INTO dbo.ExternalReferenceEntry([TaskId], [TaskActionId] ,[TableReferenceName] ,[EntityName], [PartitionKey], [RowKey], [IsRemoved]) " +
                    $"select st.*, tickets.*, 0 FROM {fromSql}, (values (@TaskId, @TaskActionId, @TableReferenceName, @EntityName)) as st(taskId, taskActionId, tableRef, entityName)";

                dParam.Add($"@EntityName", typeof(TicketEntity).Name);
                dParam.Add($"@TaskId", taskEntry.Id);
                dParam.Add($"@TaskActionId", taskAction.Id);
                dParam.Add($"@TableReferenceName", typeof(TicketEntity).Name);

                await connection.ExecuteAsync(sql, dParam);
            }

            return taskEntry;
        }

        public async Task InsertNew(TaskEntry entry)
        {
            using (var connection = new SqlConnection(Environment.GetEnvironmentVariable("ConnectionString")))
            {
                var items = await connection.QueryAsync<TaskEntry>("Select * from dbo.TaskEntry");
            }
        }
    }
}
