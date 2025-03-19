using Azure;
using Azure.Data.Tables;
using EntityDto;
using EntityDto.ProductCodes;
using System.Diagnostics.CodeAnalysis;

namespace RepositoryContract.ProductCodes
{
    public class ProductStatsEntry : ProductStats, ITableEntity, IEqualityComparer<ProductStatsEntry>
    {
        public ETag ETag { get; set; }

        public bool Equals(ProductStatsEntry? x, ProductStatsEntry? y)
        {
            return base.Equals(x, y);
        }

        public int GetHashCode([DisallowNull] ProductStatsEntry obj)
        {
            return base.GetHashCode(obj);
        }
    }
}
