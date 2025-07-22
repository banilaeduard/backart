using Azure;
using Azure.Data.Tables;
using EntityDto.Config;
using System.Diagnostics.CodeAnalysis;

namespace RepositoryContract.Cfg
{
    public class CategoryEntity : Category, ITableEntity, IEqualityComparer<CategoryEntity>

    {
        public ETag ETag { get; set; }

        public bool Equals(CategoryEntity? x, CategoryEntity? y)
        {
            return base.Equals(x, y);
        }

        public int GetHashCode([DisallowNull] CategoryEntity obj)
        {
            return base.GetHashCode(obj);
        }
    }
}
