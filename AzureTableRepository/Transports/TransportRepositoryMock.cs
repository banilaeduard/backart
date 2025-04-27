using RepositoryContract.ExternalReferenceGroup;
using RepositoryContract.Transports;

namespace AzureTableRepository.Transports
{
    public class TransportRepositoryMock : ITransportRepository
    {
        public async Task DeleteTransport(int transportId)
        {
            //throw new NotImplementedException();
        }

        public async Task<TransportEntry> GetTransport(int transportId)
        {
            return new TransportEntry()
            {
                CarPlateNumber = "ABC123",
                Created = DateTime.UtcNow,
                CurrentStatus = "InProgress",
                Delivered = DateTime.UtcNow.AddDays(1),
                Description = "Test",
                Distance = 100,
                DriverName = "John Doe",
                ExternalItemId = "EXT123",
                ExternalReferenceEntries = new List<ExternalReferenceGroupEntry>
                {
                    new ExternalReferenceGroupEntry
                    {
                        ExternalGroupId = "EXT123",
                        Id = transportId,
                        PartitionKey = Environment.MachineName ?? "default",
                        RowKey = Guid.NewGuid().ToString()
                    }
                },
                FuelConsumption = 10,
                Id = transportId,
            };
        }

        public async Task<List<TransportEntry>> GetTransports(DateTime? since = null, int? pageSize = null)
        {
            return [new TransportEntry()
            {
                CarPlateNumber = "ABC123",
                Created = DateTime.UtcNow,
                CurrentStatus = "InProgress",
                Delivered = DateTime.UtcNow.AddDays(1),
                Description = "Test",
                Distance = 100,
                DriverName = "John Doe",
                ExternalItemId = "EXT123",
                FuelConsumption = 10,
                Id = 1,
            }];
        }

        public Task<List<ExternalReferenceGroupEntry>> HandleExternalAttachmentRefs(List<ExternalReferenceGroupEntry>? externalReferenceGroupEntries, int transportId, int[] deteledAttachments)
        {
            throw new NotImplementedException();
        }

        public Task<TransportEntry> SaveTransport(TransportEntry transportEntry)
        {
            throw new NotImplementedException();
        }

        public Task<TransportEntry> UpdateTransport(TransportEntry transportEntry, int[] deletedTransportItems = null)
        {
            throw new NotImplementedException();
        }
    }
}
