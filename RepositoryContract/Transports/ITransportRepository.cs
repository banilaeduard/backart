using RepositoryContract.ExternalReferenceGroup;

namespace RepositoryContract.Transports
{
    public interface ITransportRepository
    {
        public Task<TransportEntry> UpdateTransport(TransportEntry transportEntry, int[] deletedTransportItems = null);
        public Task<TransportEntry> SaveTransport(TransportEntry transportEntry);
        public Task<TransportEntry> GetTransport(int transportId);
        public Task<List<ExternalReferenceGroupEntry>> HandleExternalAttachmentRefs(List<ExternalReferenceGroupEntry>? externalReferenceGroupEntries, int transportId, int[] deteledAttachments);
        public Task DeleteTransport(int transportId);
        public Task<List<TransportEntry>> GetTransports(DateTime? since = null, int? pageSize = null);
    }
}
