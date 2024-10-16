﻿using RepositoryContract.Tasks;

namespace SqlTableRepository.Tasks
{
    internal static class TaskSql
    {
        internal static string UpsertExternalReference(string fromSql) => $@"
                                       WITH dif as (
                                            SELECT @TableReferenceName as TableName, tickets.PartitionKey, tickets.RowKey, tickets.ExternalGroupId, tickets.Date
                                            FROM {fromSql}
                                            LEFT JOIN [dbo].[ExternalReferenceGroup] erg on tickets.PartitionKey = erg.PartitionKey AND tickets.RowKey = erg.RowKey AND erg.TableName = @TableReferenceName
                                            WHERE erg.G_Id IS NULL
                                        ) INSERT INTO [dbo].[ExternalReferenceGroup](TableName, PartitionKey, RowKey, ExternalGroupId, Date)
                                          SELECT * from dif;";
        internal static string InsertEntityRef(string fromSql) => $@"
                                INSERT INTO dbo.ExternalReferenceEntry([TaskId], [TaskActionId], [GroupId], [IsRemoved])
                                SELECT st.*, erg.G_Id, 0 
                                FROM {fromSql}
                                JOIN (values (@TaskId, @TaskActionId)) as st(taskId, taskActionId) on 1 = 1
                                JOIN [dbo].[ExternalReferenceGroup] erg on tickets.PartitionKey = erg.PartitionKey AND tickets.RowKey = erg.RowKey AND erg.TableName = @TableReferenceName";

        internal readonly static string InsertTask = $@"INSERT INTO dbo.TaskEntry(Name, Details, LocationCode)
                                      OUTPUT INSERTED.*
                                      VALUES(@Name, @Details, @LocationCode)
                                      INSERT INTO dbo.TaskAction([TaskId],[Description])
                                      OUTPUT INSERTED.*
                                      SELECT SCOPE_IDENTITY(), 'Created from tickets[' + @Count + ']';";

        internal readonly static (string sql, Func<dynamic, dynamic, ExternalReferenceEntry> mapper, string splitOn) ExternalRefs =
            ($@"SELECT er.*, erg.* FROM dbo.ExternalReferenceEntry er
                    JOIN dbo.TaskEntry t on er.TaskId = t.Id
                    JOIN dbo.ExternalReferenceGroup erg on er.GroupId = erg.G_Id",
            (d, eg) => new ExternalReferenceEntry()
            {
                Created = d.Created,
                TableName = eg.TableName,
                ExternalGroupId = eg.ExternalGroupId,
                GroupId = d.GroupId,
                Id = eg.G_Id,
                IsRemoved = d.IsRemoved,
                PartitionKey = eg.PartitionKey,
                RowKey = eg.RowKey,
                TaskActionId = d.TaskActionId,
                TaskId = d.TaskId,
                Date = eg.Date
            }, "G_Id");
    }
}