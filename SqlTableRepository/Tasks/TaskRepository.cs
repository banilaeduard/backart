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
                string taskAction = "SELECT ta.* FROM dbo.TaskAction ta JOIN dbo.TaskEntry t on ta.TaskId = t.Id WHERE t.IsClosed = 0";
                string taskExternalReferenceEntry = $@"SELECT er.*, erg.* FROM dbo.ExternalReferenceEntry er
                    JOIN dbo.TaskEntry t on er.TaskId = t.Id
                    JOIN dbo.ExternalReferenceGroup erg on er.GroupId = erg.G_Id";

                var multi = await connection.QueryMultipleAsync($"{taskSql};{taskAction};");

                tasks = [.. multi.Read<TaskEntry>()];
                actions = [.. multi.Read<TaskAction>()];

                externalRef = (await connection.QueryAsync<dynamic, dynamic, ExternalReferenceEntry>(taskExternalReferenceEntry,
                    (d, eg) => new ExternalReferenceEntry()
                    {
                        Created = d.Created,
                        TableReferenceName = eg.TableName,
                        ExternalGroupId = eg.ExternalGroupId,
                        GroupId = d.GroupId,
                        Id = eg.G_Id,
                        IsRemoved = d.IsRemoved,
                        PartitionKey = eg.PartitionKey,
                        RowKey = eg.RowKey,
                        TaskActionId = d.TaskActionId,
                        TaskId = d.TaskId
                    }
                    ,
                    splitOn: "G_Id")).ToList();
            }

            return [.. tasks.Select(task => TaskEntry.From(task, actions, externalRef))];
        }

        public async Task<TaskEntry> SaveTask(TaskEntry task)
        {
            DynamicParameters dParam = new();
            var count = task.ExternalReferenceEntries.Count();
            string fromSql = "";
            if (count > 0)
            {
                fromSql = task.ExternalReferenceEntries.FromValues(dParam, "tickets", t => t.PartitionKey, t => t.RowKey, t => t.ExternalGroupId);
            }
            TaskEntry taskEntry;
            TaskAction taskAction;
            List<ExternalReferenceEntry>? externalRef = null;         

            using (var connection = new SqlConnection(Environment.GetEnvironmentVariable("ConnectionString")))
            {
                var result = await connection.QueryMultipleAsync(
                                      $@"INSERT INTO dbo.TaskEntry(Name, Details, LocationCode)
                                      OUTPUT INSERTED.*
                                      VALUES(@Name, @Details, @LocationCode)
                                      INSERT INTO dbo.TaskAction([TaskId],[Description])
                                      OUTPUT INSERTED.*
                                      SELECT SCOPE_IDENTITY(), 'Created from tickets[' + @Count + ']';"
                                      , param: new
                                      {
                                          Count = count.ToString(),
                                          task.Name,
                                          task.Details,
                                          task.LocationCode,
                                      });
                taskEntry = result.Read<TaskEntry>().First();
                taskAction = result.Read<TaskAction>().First();

                if (count > 0)
                {
                    var upsertExternalGroup = $@"WITH dif as (
                                            SELECT @TableReferenceName as TableName, tickets.PartitionKey, tickets.RowKey, tickets.ExternalGroupId
                                            FROM {fromSql}
                                            LEFT JOIN [dbo].[ExternalReferenceGroup] erg on tickets.PartitionKey = erg.PartitionKey AND tickets.RowKey = erg.RowKey AND erg.TableName = @TableReferenceName
                                            WHERE erg.G_Id IS NULL
                                        ) INSERT INTO [dbo].[ExternalReferenceGroup](TableName, PartitionKey, RowKey, ExternalGroupId)
                                          select * from dif";

                    dParam.Add($"@TableReferenceName", typeof(TicketEntity).Name);
                    await connection.ExecuteAsync(upsertExternalGroup, dParam);

                    var sql = $@"INSERT INTO dbo.ExternalReferenceEntry([TaskId], [TaskActionId], [GroupId], [IsRemoved])
                    select st.*, erg.G_Id, 0 
                    FROM {fromSql}
                        JOIN (values (@TaskId, @TaskActionId)) as st(taskId, taskActionId) on 1 = 1
                        JOIN [dbo].[ExternalReferenceGroup] erg on tickets.PartitionKey = erg.PartitionKey AND tickets.RowKey = erg.RowKey AND erg.TableName = @TableReferenceName";

                    dParam.Add($"@TaskId", taskAction.TaskId);
                    dParam.Add($"@TaskActionId", taskAction.Id);

                    await connection.ExecuteAsync(sql, dParam);

                    string taskExternalReferenceEntry = $@"SELECT er.*, erg.* FROM dbo.ExternalReferenceEntry er
                    JOIN dbo.TaskEntry t on er.TaskId = t.Id
                    JOIN dbo.ExternalReferenceGroup erg on er.GroupId = erg.G_Id
                    WHERE t.Id = @TaskId";

                    externalRef = (await connection.QueryAsync<dynamic, dynamic, ExternalReferenceEntry>(taskExternalReferenceEntry,
                        (d, eg) => new ExternalReferenceEntry()
                        {
                            Created = d.Created,
                            TableReferenceName = eg.TableName,
                            ExternalGroupId = eg.ExternalGroupId,
                            GroupId = d.GroupId,
                            Id = eg.G_Id,
                            IsRemoved = d.IsRemoved,
                            PartitionKey = eg.PartitionKey,
                            RowKey = eg.RowKey,
                            TaskActionId = d.TaskActionId,
                            TaskId = d.TaskId
                        }
                        ,
                        splitOn: "G_Id", param: new { TaskId = taskEntry.Id })).ToList();
                }
            }
            return TaskEntry.From(task, [taskAction], externalRef);
        }
    }
}