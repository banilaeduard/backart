namespace ProjectKeys
{
    public static class KeyCollection
    {
        public static readonly string ConnectionString;
        public static readonly string ExternalServer;
        public static readonly string PathToSql;
        public static readonly string StorageConnection;
        public static readonly string FileShareName;
        public static readonly string BlobShareName;
        public static readonly string InstrumentationConnectionString;
        static KeyCollection()
        {
#if DEBUG
            ConnectionString = "ConnectionString";
#else
            ConnectionString = "ConnectionString_prod";
#endif
            ExternalServer = "external_sql_server";
            PathToSql = "path_to_sql";
            StorageConnection = "storage_connection";
            FileShareName = "file_share_name";
            BlobShareName = "blob_share_name";
            InstrumentationConnectionString = "insight_connection_string";
        }
    }
}
