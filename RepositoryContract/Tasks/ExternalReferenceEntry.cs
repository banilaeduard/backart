using EntityDto.Tasks;
using System.Diagnostics.CodeAnalysis;

namespace RepositoryContract.Tasks
{
    public class ExternalReferenceEntry : ExternalReference
    {
        public bool Equals(ExternalReferenceEntry? x, ExternalReferenceEntry? y)
        {
            return base.Equals(x, y);
        }

        public int GetHashCode([DisallowNull] ExternalReferenceEntry obj)
        {
            return base.GetHashCode(obj);
        }
    }
}
