namespace ServiceInterface.Storage
{
    public interface IMetadataService
    {
        Task SetMetadata(string fName, string? leaseId, IDictionary<string, string> metadata = null, params string[] args);
        Task<IDictionary<string, string>> GetMetadata(string fName, params string[] args);
        Task DeleteMetadata(string fName, params string[] args);
        Task<ILeaseClient> GetLease(string fName, params string[] args);
    }
}
