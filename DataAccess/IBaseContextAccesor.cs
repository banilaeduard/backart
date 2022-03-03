namespace DataAccess
{
    public interface IBaseContextAccesor
    {
        string TenantId { get; }
        string DataKeyLocation { get; }
        string DataKeyName { get; }
        string DataKeyId { get; }
        bool IsAdmin { get; }
        bool disableFiltering { get; }
    }
}
