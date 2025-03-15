namespace ServiceInterface.Storage
{
    public interface ILeaseClient
    {
        string LeaseId {  get; }
        Task Acquire(TimeSpan time);
        Task Release();
    }
}
