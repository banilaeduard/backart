using Dapper;
using EntityDto.Tasks;
using Microsoft.Data.SqlClient;
using ProjectKeys;
using RepositoryContract.Tasks;
using RepositoryContract.Tickets;
using System.Globalization;

namespace SqlTableRepository.Tasks
{
    public class TaskRepository : ITaskRepository
    {
        public async Task MarkAsClosed(int[] taskIds)
        {
            using (var connection = new SqlConnection(Environment.GetEnvironmentVariable(KeyCollection.ConnectionString)))
            {
                string markAsClosed = $"UPDATE dbo.TaskEntry SET IsClosed = 1 where Id in @taskIds";
                await connection.ExecuteAsync($"{markAsClosed};", new { taskIds });
            }
        }

        public async Task DeleteTask(int Id)
        {
            using (var connection = new SqlConnection(Environment.GetEnvironmentVariable(KeyCollection.ConnectionString)))
            {
                string taskSql = $"DELETE FROM dbo.TaskEntry WHERE Id = {Id}";
                string taskAction = $"DELETE FROM dbo.TaskAction WHERE TaskId = {Id}";
                string taskExternalReferenceEntry = $"DELETE FROM dbo.ExternalReferenceEntry WHERE TaskId = {Id}";
                await connection.ExecuteAsync($"{taskExternalReferenceEntry}; {taskAction}; {taskSql}");
            }
        }

        public async Task<IList<TaskEntry>> GetTasks(TaskInternalState status)
        {
            return await GetTasksInternal(status);
        }

        public async Task<TaskEntry> SaveTask(TaskEntry task)
        {
            var count = task.ExternalReferenceEntries?.Count();

            TaskEntry taskEntry;
            TaskActionEntry taskAction;
            List<ExternalReferenceEntry>? externalRef = null;

            using (var connection = new SqlConnection(Environment.GetEnvironmentVariable(KeyCollection.ConnectionString)))
            {
                var result = await connection.QueryMultipleAsync(TaskSql.InsertTask
                                      , param: new
                                      {
                                          Count = count.ToString(),
                                          task.Name,
                                          task.Details,
                                          task.LocationCode,
                                          TaskDate = task.TaskDate.ToUniversalTime(),
                                      });
                taskEntry = result.Read<TaskEntry>().First();
                taskAction = result.Read<TaskActionEntry>().First();

                if (count > 0)
                {
                    DynamicParameters dParam = new();
                    dParam.Add($"@TaskId", taskAction.TaskId);
                    dParam.Add($"@TaskActionId", taskAction.Id);

                    string fromSql = task.ExternalReferenceEntries.FromValues(dParam, "tickets", t => t.PartitionKey, t => t.RowKey, t => t.ExternalGroupId, t => t.Date, t => t.TableName);

                    externalRef = (await connection.QueryAsync($"{TaskSql.UpsertExternalReference(fromSql)}; {TaskSql.InsertEntityRef(fromSql)}; {TaskSql.ExternalRefs.sql} WHERE ta.TaskId = @TaskId",
                        TaskSql.ExternalRefs.mapper
                        , param: dParam
                        , splitOn: TaskSql.ExternalRefs.splitOn)
                        ).ToList();
                }
            }
            return TaskEntry.From(taskEntry, [taskAction], externalRef);
        }

        public async Task<TaskEntry> UpdateTask(TaskEntry task)
        {
            dynamic taskResult;
            TaskActionEntry taskAction = new();
            List<ExternalReferenceEntry>? externalRef = null;

            using (var connection = new SqlConnection(Environment.GetEnvironmentVariable(KeyCollection.ConnectionString)))
            {
                string updateTask = $@"UPDATE dbo.TaskEntry SET Name = @Name, Details = @Details, IsClosed = @IsClosed, TaskDate = @TaskDate
                    OUTPUT INSERTED.*, DELETED.Details as old_details, DELETED.Name as old_name, DELETED.IsClosed as old_isclosed, DELETED.TaskDate as old_taskdate
                    WHERE Id = @TaskId";
                taskResult = await connection.QueryFirstAsync<dynamic>(updateTask, new { TaskId = task.Id, task.Name, task.Details, task.IsClosed, TaskDate = task.TaskDate.ToUniversalTime() });

                var newExternal = task.ExternalReferenceEntries.Where(e => e.Id < 1).ToList();
                if (taskResult.Details != taskResult.old_details || taskResult.Name != taskResult.old_name
                    || taskResult.IsClosed != taskResult.old_isclosed || taskResult.TaskDate != taskResult.old_taskdate || newExternal.Count > 0)
                {
                    string insertAction = $"INSERT INTO dbo.TaskAction(TaskId, Description, [External]) OUTPUT INSERTED.* VALUES (@TaskId, @Description, @External)";
                    string descriptionStart = taskResult.IsClosed != taskResult.old_isclosed ? "Mark as closed" : "Update task properties";
                    string name = taskResult.Name != taskResult.old_name ? $"Name: {taskResult.old_name}" : "";
                    string taskDate = taskResult.TaskDate != taskResult.old_taskdate ? $"Task Date: {taskResult.old_taskdate.ToString(CultureInfo.InvariantCulture)}" : "";
                    string details = taskResult.Details != taskResult.old_details ? $"Details: {taskResult.old_details}" : "";

                    var desc = $"{descriptionStart}.{taskDate}; {name}; {details}";
                    ExternalReferenceEntry? externalAction = null;
                    if ((externalAction = newExternal.FirstOrDefault(x => x.Action.HasValue && x.Action.Value == ActionType.External)) != null)
                    {
                        desc = string.IsNullOrEmpty(desc?.Replace(";", "")) ? "EXTERNAL_ACTION" : $"EXTERNAL;{desc}";
                    }

                    taskAction = await connection.QueryFirstAsync<TaskActionEntry>($"{insertAction};", new { TaskId = task.Id, Description = desc, External = externalAction != null, Value = taskResult.IsClosed ? -1 : 1 });
                    if (newExternal.Count > 0)
                    {
                        DynamicParameters dParam = new();
                        dParam.Add($"@TaskId", taskAction.TaskId);
                        dParam.Add($"@TaskActionId", taskAction.Id);

                        string fromSql = newExternal.FromValues(dParam, "tickets", t => t.PartitionKey, t => t.RowKey, t => t.ExternalGroupId, t => t.Date, t => t.TableName);

                        externalRef = (await connection.QueryAsync($"{TaskSql.UpsertExternalReference(fromSql)}; {TaskSql.InsertEntityRef(fromSql)}; {TaskSql.ExternalRefs.sql} WHERE ta.TaskId = @TaskId",
                            TaskSql.ExternalRefs.mapper
                            , param: dParam
                            , splitOn: TaskSql.ExternalRefs.splitOn)
                            ).ToList();
                    }
                }
            }
            return (await GetTasksInternal(TaskInternalState.All, [task.Id]))[0];
        }

        public async Task DeleteTaskExternalRef(int taskId, string partitionKey, string rowKey)
        {
            using (var connection = new SqlConnection(Environment.GetEnvironmentVariable(KeyCollection.ConnectionString)))
            {
                await connection.ExecuteAsync($@"
                    UPDATE dbo.ExternalReferenceEntry SET IsRemoved = 1
                    FROM dbo.ExternalReferenceGroup erg
                    JOIN dbo.ExternalReferenceEntry er on er.GroupId = erg.G_Id
                    WHERE erg.PartitionKey = @PartitionKey and erg.RowKey = @RowKey and er.TaskId = @TaskId", new
                {
                    PartitionKey = partitionKey,
                    RowKey = rowKey,
                    TaskId = taskId
                });
            }
        }

        public async Task<IList<TaskEntry>> GetTasks(int[] taskIds)
        {
            return await GetTasksInternal(TaskInternalState.All, taskIds);
        }

        public async Task AcceptExternalRef(int taskId, string partitionKey, string rowKey)
        {
            using (var connection = new SqlConnection(Environment.GetEnvironmentVariable(KeyCollection.ConnectionString)))
            {
                await connection.ExecuteAsync($@"update dbo.TaskAction
                                                Set Accepted = 1
                                                from dbo.TaskAction ta
                                                join dbo.ExternalReferenceEntry er on ta.Id = er.TaskActionId
                                                join dbo.ExternalReferenceGroup erg on erg.G_Id = er.GroupId
                                                where erg.PartitionKey = @PartitionKey and erg.RowKey = @RowKey and erg.TableName = @TableName and ta.TaskId = @TaskId"
                                                , new
                                                {
                                                    PartitionKey = partitionKey,
                                                    RowKey = rowKey,
                                                    TaskId = taskId,
                                                    TableName = nameof(TicketEntity)
                                                });
            }
        }

        private async Task<IList<TaskEntry>> GetTasksInternal(TaskInternalState status, int[] TaskId = null)
        {
            IList<TaskEntry> tasks;
            IList<TaskActionEntry> actions;
            IList<ExternalReferenceEntry> externalRef;
            using (var connection = new SqlConnection(Environment.GetEnvironmentVariable(KeyCollection.ConnectionString)))
            {
                var whereIn = @$"{(TaskId?.Any() == true ? "" : GetTaskStatus(status))} {(TaskId?.Any() == true ? "t.Id in @TaskId" : "")}";
                string taskSql = $"SELECT * FROM dbo.TaskEntry t WHERE {whereIn}";
                string taskAction = $"SELECT ta.* FROM dbo.TaskAction ta JOIN dbo.TaskEntry t on ta.TaskId = t.Id WHERE {whereIn}";
                var multi = await connection.QueryMultipleAsync($"{taskSql};{taskAction};", new { TaskId });

                tasks = [.. multi.Read<TaskEntry>()];
                actions = [.. multi.Read<TaskActionEntry>()];

                var ids = tasks.Select(t => t.Id).ToArray();

                externalRef = (await connection.QueryAsync($"{TaskSql.ExternalRefs.sql} WHERE ta.TaskId in @ids ;",
                    TaskSql.ExternalRefs.mapper,
                    splitOn: TaskSql.ExternalRefs.splitOn,
                    param: new { ids })).ToList();
            }
            var taskList = tasks.Select(task => TaskEntry.From(task, actions, externalRef)).ToList();

            return taskList;
        }

        private string GetTaskStatus(TaskInternalState status)
        {
            switch (status)
            {
                case TaskInternalState.All: return "1 = 1";
                case TaskInternalState.Closed: return "IsClosed = 1";
                case TaskInternalState.Open: return "IsClosed = 0 ORDER BY Created DESC";
            }
            return "";
        }
    }
}
