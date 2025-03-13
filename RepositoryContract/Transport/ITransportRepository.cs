namespace RepositoryContract.Transport
{
    public interface ITransportRepository
    {
        public Task<TransportEntry> UpdateTransport(TransportEntry transportEntry);
        public Task<TransportEntry> SaveTransport(TransportEntry transportEntry);
        public Task<TransportEntry> GetTransport(int transportId);
        public Task<List<TransportEntry>> GetTransports();
    }
}
