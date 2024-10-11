using Dapper;
using Microsoft.Data.SqlClient;
using RepositoryContract.Tasks;
using RepositoryContract.Tickets;

namespace SqlTableRepository.Tasks
{
    public class TaskRepository : ITaskRepository
    {
        public async Task DeleteTask(int Id)
        {
            using (var connection = new SqlConnection(Environment.GetEnvironmentVariable("ConnectionString")))
            {
                string taskSql = $"DELETE FROM dbo.TaskEntry WHERE Id = {Id}";
                string taskAction = $"DELETE FROM dbo.TaskAction WHERE TaskId = {Id}";
                string taskExternalReferenceEntry = $"DELETE FROM dbo.ExternalReferenceEntry WHERE TaskId = {Id}";

                await connection.ExecuteAsync($"BEGIN TRANSACTION; {taskExternalReferenceEntry}; {taskAction}; {taskSql} COMMIT;");
            }
        }

        public async Task<IList<TaskEntry>> GetActiveTasks()
        {
            IList<TaskEntry> tasks;
            IList<TaskAction> actions;
            IList<ExternalReferenceEntry> externalRef;
            using (var connection = new SqlConnection(Environment.GetEnvironmentVariable("ConnectionString")))
            {
                string taskSql = "SELECT * FROM dbo.TaskEntry WHERE IsClosed = 0";
                string taskAction = "SELECT * FROM dbo.TaskAction ta JOIN dbo.TaskEntry t on ta.TaskId = t.Id WHERE t.IsClosed = 0";
                string taskExternalReferenceEntry = "SELECT * FROM dbo.ExternalReferenceEntry er JOIN dbo.TaskEntry t on er.TaskId = t.Id WHERE t.IsClosed = 0";

                var multi = await connection.QueryMultipleAsync($"{taskSql};{taskAction};{taskExternalReferenceEntry};");

                tasks = [.. multi.Read<TaskEntry>()];
                actions = [.. multi.Read<TaskAction>()];
                externalRef = [.. multi.Read<ExternalReferenceEntry>()];
            }

            return [.. tasks.Select(task => TaskEntry.From(task, actions, externalRef))];
        }

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
                                      $"INSERT INTO dbo.TaskEntry(Name, Details, LocationCode) " +
                                      $"OUTPUT INSERTED.*" +
                                      $"VALUES(@Name, @Details, @LocationCode);"
                                      , new { Name = tickets[0].From, Details = tickets[0].Description, LocationCode = tickets[0].LocationCode });

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
    }
}
