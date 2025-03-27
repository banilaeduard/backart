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
                                                           [DriverName] ,
                                                           [CarPlateNumber] ,
                                                           [Distance] ,
                                                           [FuelConsumption] ,
                                                           [CurrentStatus] ,
                                                           [ExternalItemId] ,
                                                           [Created] ,
                                                           [Delivered]
                                                    FROM [dbo].[Transport]";
        internal static string DeleteTransport(int transportId) => $@"DELETE FROM dbo.TransportItems WHERE TransportId = {transportId};
                                                         DELETE FROM dbo.Transport WHERE Id = {transportId};";
        internal static string GetTransportItems(int transportId) => $@"SELECT * FROM [dbo].[TransportItems] WHERE TransportId = {transportId};";

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
                    ti.ExternalItemId2 = a.ExternalItemId2
                FROM TransportItems ti
                INNER JOIN dif a ON ti.ItemId = a.ItemId;" : "";
    }
}