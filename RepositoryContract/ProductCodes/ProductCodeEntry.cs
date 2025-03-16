using Azure;
using Azure.Data.Tables;
using EntityDto;
using EntityDto.ProductCodes;
using System.Diagnostics.CodeAnalysis;

namespace AzureSerRepositoryContract.ProductCodesvices
{
    public class ProductCodeEntry : ProductCode, ITableEntity, ITableEntryDto<ProductCodeEntry>
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
