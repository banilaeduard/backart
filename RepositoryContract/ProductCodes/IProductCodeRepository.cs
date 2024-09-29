using AzureSerRepositoryContract.ProductCodesvices;
using System.Linq.Expressions;

namespace RepositoryContract.ProductCodes
{
    public interface IProductCodeRepository
    {
        Task<IList<ProductCodeEntry>> GetProductCodes(Func<ProductCodeEntry, bool> expr, string? table = null);
        Task<IList<ProductCodeEntry>> GetProductCodes(string? table = null);
    }
}
