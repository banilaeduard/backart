namespace SqlTableRepository.ExternalReferenceGroup
{
    internal static class ExternalRefSql
    {
        internal static string UpsertExternalRef(string fromSql, string fromAlias) => $@"
                                                WITH OLDENTRIES as (
                                                    SELECT {fromAlias}.* 
                                                    FROM {fromSql}
                                                    LEFT JOIN [dbo].[ExternalReferenceGroup] ti 
	                                                on ti.Id = {fromAlias}.Id and ti.PartitionKey = {fromAlias}.PartitionKey and ti.TableName = {fromAlias}.TableName and ti.ExternalGroupId = {fromAlias}.ExternalGroupId
                                                    WHERE ti.G_Id IS NOT NULL
                                                )
                                                UPDATE ti
                                                    SET 
                                                        ti.TableName = a.TableName,
                                                        ti.PartitionKey = a.PartitionKey,
                                                        ti.RowKey = a.RowKey,
                                                        ti.ExternalGroupId = a.ExternalGroupId,
                                                        ti.EntityType = a.EntityType
                                                    OUTPUT DELETED.* 
                                                    FROM ExternalReferenceGroup ti
                                                    INNER JOIN OLDENTRIES a ON ti.Id = a.Id and ti.PartitionKey = a.PartitionKey and ti.TableName = a.TableName and ti.ExternalGroupId = a.ExternalGroupId;
                                                WITH NEWENTRIES as (
                                                    SELECT {fromAlias}.* 
                                                    FROM {fromSql}
                                                    LEFT JOIN [dbo].[ExternalReferenceGroup] ti 
	                                                on ti.Id = {fromAlias}.Id and ti.PartitionKey = {fromAlias}.PartitionKey and ti.TableName = {fromAlias}.TableName and ti.ExternalGroupId = {fromAlias}.ExternalGroupId
                                                    WHERE ti.G_Id IS NULL
                                                )
                                                INSERT INTO [dbo].[ExternalReferenceGroup]([TableName],[PartitionKey],[RowKey],[ExternalGroupId],[EntityType],[Id], Ref_count)
                                                    SELECT TableName, PartitionKey, RowKey, ExternalGroupId, EntityType, Id, 0
                                                    FROM NEWENTRIES;";

        internal static string DeleteExternalRef => "DELETE FROM [dbo].[ExternalReferenceGroup] WHERE G_Id IN @externalRefs;";
    }
}
