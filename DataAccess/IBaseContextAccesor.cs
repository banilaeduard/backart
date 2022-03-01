namespace DataAccess
{
    public interface IBaseContextAccesor
    {
        string TenantId { get; }
        string DataKey { get; }
        bool IsAdmin { get; }
        bool disableFiltering { get; }
    }
}
