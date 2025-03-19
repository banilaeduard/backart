using Azure;
using Azure.Data.Tables;
using EntityDto.ProductCodes;
using System.Diagnostics.CodeAnalysis;

namespace RepositoryContract.ProductCodes
{
    public class ProductCodeEntry : ProductCode, ITableEntity, IEqualityComparer<ProductCodeEntry>
    {
        public ETag ETag { get; set; }

        public bool Equals(ProductCodeEntry? x, ProductCodeEntry? y)
        {
            return base.Equals(x, y);
        }

        public int GetHashCode([DisallowNull] ProductCodeEntry obj)
        {
            return base.GetHashCode(obj);
        }
    }
}
