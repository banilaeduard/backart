using AzureSerRepositoryContract.ProductCodesvices;

namespace WebApi.Models
{
    public class ProductModel : ProductCodeEntry
    {
        public List<ProductStatsModel> Stats { get; set; }
    }
}
