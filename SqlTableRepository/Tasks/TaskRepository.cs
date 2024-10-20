using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Caching.Memory;
using RepositoryContract.Tasks;
using RepositoryContract.Tickets;

namespace SqlTableRepository.Tasks
{
    public class TaskRepository : ITaskRepository
    {
        private static MemoryCache _taskCache = new(new MemoryCacheOptions() { ExpirationScanFrequency = TimeSpan.FromMinutes(2) });

        public async Task DeleteTask(int Id)
        {
            using (var connection = new SqlConnection(Environment.GetEnvironmentVariable("ConnectionString")))
            {
                string taskSql = $"DELETE FROM dbo.TaskEntry WHERE Id = {Id}";
                string taskAction = $"DELETE FROM dbo.TaskAction WHERE TaskId = {Id}";
                string taskExternalReferenceEntry = $"DELETE FROM dbo.ExternalReferenceEntry WHERE TaskId = {Id}";
                await connection.ExecuteAsync($"{taskExternalReferenceEntry}; {taskAction}; {taskSql}");
            }
            _taskCache.Clear();
        }

        public async Task<IList<TaskEntry>> GetActiveTasks()
        {
            return await GetTasksInternal(TaskStatus.Open);
        }

        public async Task<IList<ExternalReferenceEntry>> GetExternalReferences()
        {
            var key = $"GetExternalReferences";
            if (!_taskCache.TryGetValue(key, out IList<ExternalReferenceEntry>? tasksExternalCache) || tasksExternalCache == null)
            {
                string taskExternalReferenceEntry = $@"SELECT * FROM dbo.ExternalReferenceGroup WHERE Ref_count > 0";
                using (var connection = new SqlConnection(Environment.GetEnvironmentVariable("ConnectionString")))
                {
                    var list = (await connection.QueryAsync<ExternalReferenceEntry>(taskExternalReferenceEntry)).ToList();
                    if (list.Any())
                        return _taskCache.Set(key, list, new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromMinutes(5)));
                    return [];
                }
            }
            return tasksExternalCache;
        }

        public async Task<TaskEntry> SaveTask(TaskEntry task)
        {
            var count = task.ExternalReferenceEntries?.Count();

            TaskEntry taskEntry;
            TaskAction taskAction;
            List<ExternalReferenceEntry>? externalRef = null;

            using (var connection = new SqlConnection(Environment.GetEnvironmentVariable("ConnectionString")))
            {
                var result = await connection.QueryMultipleAsync(TaskSql.InsertTask
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
                    DynamicParameters dParam = new();
                    dParam.Add($"@TableReferenceName", nameof(TicketEntity));
                    dParam.Add($"@TaskId", taskAction.TaskId);
                    dParam.Add($"@TaskActionId", taskAction.Id);

                    string fromSql = task.ExternalReferenceEntries.FromValues(dParam, "tickets", t => t.PartitionKey, t => t.RowKey, t => t.ExternalGroupId, t => t.Date);

                    externalRef = (await connection.QueryAsync($"{TaskSql.UpsertExternalReference(fromSql)}; {TaskSql.InsertEntityRef(fromSql)}; {TaskSql.ExternalRefs.sql} WHERE t.Id = @TaskId",
                        TaskSql.ExternalRefs.mapper
                        , param: dParam
                        , splitOn: TaskSql.ExternalRefs.splitOn)
                        ).ToList();
                }
            }
            _taskCache.Clear();
            return TaskEntry.From(task, [taskAction], externalRef);
        }

        public async Task<TaskEntry> UpdateTask(TaskEntry task)
        {
            using (var connection = new SqlConnection(Environment.GetEnvironmentVariable("ConnectionString")))
            {
                string updateTask = $"UPDATE dbo.TaskEntry SET Name = @Name, Details = @Details, IsClosed = @IsClosed " +
                    $"OUTPUT INSERTED.*, DELETED.Details as old_details, DELETED.Name as old_name, DELETED.IsClosed as old_isclosed " +
                    $"WHERE Id = @TaskId";
                var taskResult = await connection.QueryFirstAsync<dynamic>(updateTask, new { TaskId = task.Id, task.Name, task.Details, task.IsClosed });

                if (taskResult.Details != taskResult.old_details || taskResult.Name != taskResult.old_name || taskResult.IsClosed != taskResult.old_isclosed)
                {
                    string insertAction = $"INSERT INTO dbo.TaskAction(TaskId, Description) OUTPUT INSERTED.* VALUES (@TaskId, @Description)";
                    string descriptionStart = taskResult.IsClosed != taskResult.old_isclosed ? "Mark as closed" : "Update task properties";
                    string name = taskResult.Name != taskResult.old_name ? $"Name: {taskResult.old_name}" : "";
                    string details = taskResult.Details != taskResult.old_details ? $"Details: {taskResult.old_details}" : "";

                    string refCountUpdate = "";
                    if (taskResult.IsClosed != taskResult.old_isclosed)
                    {
                        refCountUpdate = @$"UPDATE erg SET Ref_count = Ref_count + @Value 
                                            FROM dbo.ExternalReferenceGroup erg JOIN ExternalReferenceEntry er ON erg.G_Id = er.GroupId WHERE er.TaskId = @TaskId";
                    }

                    await connection.ExecuteAsync($"{insertAction};{refCountUpdate}", new { TaskId = task.Id, Description = $"{descriptionStart}. {name}; {details}", Value = taskResult.IsClosed ? -1 : 1 });
                }
            }
            _taskCache.Clear();
            return (await GetTasksInternal(TaskStatus.All, task.Id))[0];
        }

        private async Task<IList<TaskEntry>> GetTasksInternal(TaskStatus status, int? TaskId = null)
        {
            var key = $"GetActiveTasksInternal_{TaskId ?? -1}";
            if (!_taskCache.TryGetValue(key, out IList<TaskEntry>? tasksCache) || tasksCache == null)
            {
                IList<TaskEntry> tasks;
                IList<TaskAction> actions;
                IList<ExternalReferenceEntry> externalRef;
                using (var connection = new SqlConnection(Environment.GetEnvironmentVariable("ConnectionString")))
                {
                    string taskSql = $"SELECT * FROM dbo.TaskEntry WHERE {GetTaskStatus(status)} AND {(TaskId.HasValue ? "Id = @TaskId" : "@TaskId = @TaskId")}";
                    string taskAction = $"SELECT ta.* FROM dbo.TaskAction ta JOIN dbo.TaskEntry t on ta.TaskId = t.Id WHERE {GetTaskStatus(status)} AND {(TaskId.HasValue ? "t.Id = @TaskId" : "@TaskId = @TaskId")}";
                    var multi = await connection.QueryMultipleAsync($"{taskSql};{taskAction};", new { TaskId = TaskId ?? -1 });

                    tasks = [.. multi.Read<TaskEntry>()];
                    actions = [.. multi.Read<TaskAction>()];

                    externalRef = (await connection.QueryAsync($"{TaskSql.ExternalRefs.sql} WHERE {(TaskId.HasValue ? "t.Id = @TaskId" : "@TaskId = @TaskId")}",
                        TaskSql.ExternalRefs.mapper,
                        splitOn: TaskSql.ExternalRefs.splitOn,
                        param: new { TaskId = TaskId ?? -1 })).ToList();
                }
                var taskList = tasks.Select(task => TaskEntry.From(task, actions, externalRef)).ToList();

                if (taskList.Any())
                {
                    return _taskCache.Set(key, taskList, new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromMinutes(5)));
                }
                return [];
            }

            return tasksCache;
        }

        private string GetTaskStatus(TaskStatus status)
        {
            switch (status)
            {
                case TaskStatus.All: return "1 = 1";
                case TaskStatus.Closed: return "IsClosed = 1";
                case TaskStatus.Open: return "IsClosed = 0";
            }
            return "";
        }
    }

    enum TaskStatus {
        All = 3,
        Closed = 1,
        Open = 2
    }
}