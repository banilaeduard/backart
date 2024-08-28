namespace DataAccess
{
    public class NoFilterBaseContext : IBaseContextAccesor
    {
        public string TenantId => "";

        public bool IsAdmin => false;

        public bool disableFiltering => true;

        public string DataKeyLocation => "";

        public string DataKeyName => "";

        public string DataKeyId => "";
    }
}
