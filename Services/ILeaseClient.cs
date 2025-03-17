namespace ServiceInterface.Storage
{
    public interface ILeaseClient : IDisposable
    {
        string LeaseId { get; }
        Task<ILeaseClient> Acquire(TimeSpan time);
    }
}