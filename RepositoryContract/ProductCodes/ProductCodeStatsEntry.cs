using Azure;
using Azure.Data.Tables;
using EntityDto.ProductCodes;
using EntityDto;
using System.Diagnostics.CodeAnalysis;

namespace RepositoryContract.ProductCodes
{
    public class ProductCodeStatsEntry : ProductCodeStats, ITableEntity, ITableEntryDto<ProductCodeStatsEntry>
    {
        public ETag ETag { get; set; }

        public bool Equals(ProductCodeStatsEntry? x, ProductCodeStatsEntry? y)
        {
            return base.Equals(x, y);
        }

        public int GetHashCode([DisallowNull] ProductCodeStatsEntry obj)
        {
            return base.GetHashCode(obj);
        }
    }
}
