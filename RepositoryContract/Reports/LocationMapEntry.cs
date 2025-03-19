using Azure;
using EntityDto.Reports;
using System.Diagnostics.CodeAnalysis;
using Azure.Data.Tables;

namespace RepositoryContract.Report
{
    public class LocationMapEntry : LocationMap, ITableEntity
    {
        public string PartitionKey { get; set; }
        public string RowKey { get; set; }
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }

        public string Folder { get; set; }
        public string Location { get; set; }

        public bool Equals(LocationMapEntry? x, LocationMapEntry? y)
        {
            return base.Equals(x, y);
        }

        public int GetHashCode([DisallowNull] LocationMapEntry obj)
        {
            return base.GetHashCode(obj);
        }
    }
}

