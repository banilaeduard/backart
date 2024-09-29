using RepositoryContract.CommitedOrders;
using RepositoryContract.Orders;

namespace WebApi.Models
{
    public class CommitedOrdersResponse
    {
        public DispozitieLivrareEntry Entry { get; set; }
        public List<ComandaVanzareEntry> Progress { get; set; }
        public List<ComandaVanzareEntry> Pending { get; set; }
    }
}