using EntityDto.Transports;
using System.Diagnostics.CodeAnalysis;

namespace RepositoryContract.Transports
{
    public class TransportItemEntry : TransportItem
    {
        public bool Equals(TransportItemEntry? x, TransportItemEntry? y)
        {
            return base.Equals(x, y);
        }

        public int GetHashCode([DisallowNull] TransportItemEntry obj)
        {
            return base.GetHashCode(obj);
        }
    }
}