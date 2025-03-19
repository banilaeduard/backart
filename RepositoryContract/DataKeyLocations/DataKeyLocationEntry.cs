using Azure;
using Azure.Data.Tables;
using EntityDto;
using System.Diagnostics.CodeAnalysis;

namespace RepositoryContract.DataKeyLocation
{
    public class DataKeyLocationEntry: EntityDto.DataKeyLocations.DataKeyLocation, ITableEntity, IEqualityComparer<DataKeyLocationEntry>
    {
        public ETag ETag { get; set; }

        public bool Equals(DataKeyLocationEntry? x, DataKeyLocationEntry? y)
        {
            return base.Equals(x, y);
        }

        public int GetHashCode([DisallowNull] DataKeyLocationEntry obj)
        {
            return base.GetHashCode(obj);
        }
    }
}
