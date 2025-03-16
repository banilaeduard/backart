using RepositoryContract.Orders;
using RepositoryContract.ProductCodes;

namespace WebApi.Models
{
    public class OrderModel: OrderEntry
    {
        public int? Greutate { get; set; }

        public OrderModel Weight(ProductStatsEntry? weight)
        {
            if (weight != null)
                Greutate = Cantitate * int.Parse(weight.PropertyValue);
            else Greutate = 0;
            return this;
        }
    }
}
