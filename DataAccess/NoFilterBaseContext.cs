namespace DataAccess
{
    public class NoFilterBaseContext : IBaseContextAccesor
    {
        public string TenantId => "";

        public string DataKey => "";

        public bool IsAdmin => false;

        public bool disableFiltering => true;
    }
}
