using RepositoryContract.Tickets;

namespace SqlTableRepository.Transport
{
    internal class TransportSql
    {
        internal readonly static string InsertTransport = $@"INSERT INTO [dbo].[Transport](Description, DriverName, CarPlateNumber, Distance, FuelConsumption, CurrentStatus, ExternalItemId, Delivered)
                                      OUTPUT INSERTED.*
                                      VALUES(@Description, @DriverName, @CarPlateNumber, @Distance, @FuelConsumption, @CurrentStatus, @ExternalItemId, @Delivered);";

        internal static string UpdateTransport(int transportId) => $@"UPDATE [dbo].[Transport] SET
                        Description = @Description, DriverName = @DriverName, CarPlateNumber = @CarPlateNumber, Distance = @Distance, 
                        FuelConsumption = @FuelConsumption, CurrentStatus = @CurrentStatus, ExternalItemId = @ExternalItemId, Delivered = @Delivered
                        OUTPUT INSERTED.*
                        WHERE Id = {transportId};
                        ";

        internal static string GetTransports(int? topN = null) => $@"SELECT {(topN.HasValue ? $@"TOP {topN.Value}" : "")} [Id] ,
                                                           [Description] ,
                                                           [Description2] ,
                                                           [DriverName] ,
                                                           [CarPlateNumber] ,
                                                           [Distance] ,
                                                           [FuelConsumption] ,
                                                           [CurrentStatus] ,
                                                           [ExternalItemId] ,
                                                           [Created] ,
                                                           [Delivered],
                                                           [HasAttachments]
                                                    FROM [dbo].[Transport]";
        internal static string DeleteTransport(int transportId) => $@"DELETE FROM dbo.TransportItems WHERE TransportId = {transportId};
                                                         DELETE FROM dbo.Transport WHERE Id = {transportId};";
        internal static string GetTransportItems(int transportId) => $@"SELECT * FROM [dbo].[TransportItems] WHERE TransportId = {transportId};";

        internal static string UpdateDesc2(int transportId) => $@"update
                                        Transport set Description2 = 
                                        (SELECT 
                                          STUFF((
                                            SELECT ', ' + ItemName
                                            FROM TransportItems
	                                        where TransportId = {transportId} and DocumentType = 1
                                            FOR XML PATH(''), TYPE
                                          ).value('.', 'NVARCHAR(MAX)'), 1, 2, '') AS JoinedNames)
                                          OUTPUT INSERTED.*
                                          where Id = {transportId}";

        internal static string DeleteTransportItems(int transportId, bool ngIf) => ngIf ? $@"DELETE FROM dbo.TransportItems WHERE TransportId = {transportId} AND ItemId in @detetedTransportItems;" : "";
        internal static string InsertMissingTransportItems(string fromSql, string fromAlias, bool ngIf) => ngIf ? $@"
            WITH dif as (
                SELECT {fromAlias}.*
                FROM {fromSql}
                LEFT JOIN [dbo].[TransportItems] ti on ti.ItemId = {fromAlias}.ItemId
                WHERE ti.ItemId IS NULL
            )
            INSERT INTO [dbo].[TransportItems](DocumentType, ItemName, ExternalItemId, ExternalItemId2, TransportId)
            SELECT DocumentType, ItemName, ExternalItemId, ExternalItemId2, TransportId FROM dif;" : "";

        internal static string UpdateTransportItems(string fromSql, string fromAlias, bool ngIf) => ngIf ? $@"
            WITH dif as (
                SELECT {fromAlias}.* 
                FROM {fromSql}
                LEFT JOIN [dbo].[TransportItems] ti on ti.ItemId = {fromAlias}.ItemId
                WHERE ti.ItemId IS NOT NULL
            )
            UPDATE ti
                SET 
                    ti.DocumentType = a.DocumentType,
                    ti.ItemName = a.ItemName,
                    ti.TransportId = a.TransportId,
                    ti.ExternalItemId = a.ExternalItemId,
                    ti.ExternalItemId2 = a.ExternalItemId2,
                    ti.ExternalReferenceId = case when a.ExternalReferenceId > 0 then a.ExternalReferenceId else ti.ExternalReferenceId end
                FROM TransportItems ti
                INNER JOIN dif a ON ti.ItemId = a.ItemId;" : "";

        internal static string GetAttachmetns(int transportId) => $@"
                                SELECT * FROM dbo.ExternalReferenceGroup 
                                WHERE Id = {transportId} AND TableName = 'Transport' AND Ref_count > 0 AND EntityType = '{nameof(AttachmentEntry)}'";


        internal static string InsertExternalAttachments(string fromSql, string fromAlias, int transportId) => $@"
            INSERT INTO [dbo].[ExternalReferenceGroup]([TableName],[PartitionKey],[RowKey],[ExternalGroupId],[EntityType],[Id], Ref_count)
            SELECT {fromAlias}.TableName, {fromAlias}.PartitionKey, {fromAlias}.RowKey, {fromAlias}.ExternalGroupId, {fromAlias}.EntityType, {fromAlias}.Id, 1
            FROM {fromSql}
            LEFT JOIN dbo.ExternalReferenceGroup erg ON erg.Id = {fromAlias}.Id AND {fromAlias}.TableName = erg.TableName AND {fromAlias}.EntityType = erg.EntityType 
                    AND {fromAlias}.PartitionKey = erg.PartitionKey AND {fromAlias}.RowKey = erg.RowKey
            WHERE erg.G_Id is NULL
            ;
            UPDATE dbo.Transport SET HasAttachments = 1 WHERE Id = {transportId};";

        internal static string EnsureAttachmentDeleted(int transportId, int[] attachmentIds) => $@"
                               UPDATE erg
                               SET erg.Ref_count = 0
                               FROM ExternalReferenceGroup erg
                               WHERE erg.Id = {transportId} AND erg.TableName = 'Transport' AND erg.Ref_count > 0 AND erg.EntityType = '{nameof(AttachmentEntry)}' AND G_Id in ({string.Join(',', attachmentIds)});
                               UPDATE dbo.Transport SET HasAttachments = 0 WHERE Id = {transportId} AND NOT EXISTS ({GetAttachmetns(transportId)});";
    }
}
