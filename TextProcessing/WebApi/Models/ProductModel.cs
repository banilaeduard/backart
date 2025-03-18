using RepositoryContract.ProductCodes;

namespace WebApi.Models
{
    public class ProductModel : ProductCodeEntry
    {
        public List<ProductStatsModel> Stats { get; set; }
    }
}
