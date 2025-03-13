namespace SqlTableRepository.Transport
{
    internal class TransportSql
    {
        internal readonly static string InsertTransport = $@"INSERT INTO dbo.Transport(Description, DriverName, CarPlateNumber, Distance, FuelConsumption, CurrentStatus, ExternalItemId)
                                      OUTPUT INSERTED.*
                                      VALUES(@Description, @DriverName, @CarPlateNumber, @Distance, @FuelConsumption, @CurrentStatus, @ExternalItemId)";

        internal static string UpdateTransport(int transportId) => $@"UPDATE dbo.Transport SET
                        Description = @Description, DriverName = @DriverName, DriverName = @DriverName, CarPlateNumber = @CarPlateNumber, Distance = @Distance, FuelConsumption = @FuelConsumption, CurrentStatus = @CurrentStatus, ExternalItemId = @ExternalItemId
                        WHERE Id = {transportId}";

        internal static string GetTransport(int transportId) => $@"SELCT * FROM dbo.Transport WHERE Id = {transportId}";
        internal static string GetTransportItems(int transportId) => $@"SELCT * FROM dbo.TransportItems WHERE TransportId = {transportId}";

        internal static string InsertMissingTransportItems(string fromSql, string fromAlias) => $@"
            WITH dif as (
                SELECT {fromAlias}.*
                FROM {fromSql}
                LEFT JOIN dbo.TransportItems ti on ti.ItemId = {fromAlias}.ItemId
                WHERE ti.ItemId IS NULL
            )
            INSERT INTO dbo.TransportItems(DocumentType, ItemName, ExternalItemId, ExternalItemId2, TransportId)
            SELECT * FROM dif;
        ";

        internal static string UpdateTransportItems(string fromSql, string fromAlias) => $@"
            WITH dif as (
                SELECT {fromAlias}.* 
                FROM {fromSql}
                LEFT JOIN dbo.TransportItems ti on ti.ItemId = {fromAlias}.ItemId
                WHERE ti.ItemId IS NOT NULL
            )
            UPDATE dbo.TransportItems (DocumentType, ItemName, ExternalItemId, ExternalItemId2, TransportId)
                SET DocumentType = a.DocumentType, ItemName = a.ItemName, TransportId = a.TransportId, ExternalItemId = a.ExternalItemId, ExternalItemId2 = a.ExternalItemId2
            FROM dif a
        ";
    }
}