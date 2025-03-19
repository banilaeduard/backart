
using EntityDto.Transports;
using System.Diagnostics.CodeAnalysis;

namespace RepositoryContract.Transports
{
    public class TransportEntry : Transport
    {
        public List<TransportItemEntry>? TransportItems { get; set; }

        public bool Equals(Transport? x, Transport? y)
        {
            throw new NotImplementedException();
        }

        public int GetHashCode([DisallowNull] Transport obj)
        {
            throw new NotImplementedException();
        }
    }
}
