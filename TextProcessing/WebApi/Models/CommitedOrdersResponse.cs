using RepositoryContract.CommitedOrders;

namespace WebApi.Models
{
    public class CommitedOrdersResponse
    {
        public List<DispozitieLivrareEntry> Entry { get; set; }

        public static IEnumerable<CommitedOrdersResponse> From(IList<DispozitieLivrareEntry> entries)
        {
            foreach (var group in entries.GroupBy(t => t.NumarIntern).OrderByDescending(t => t.Key))
            {
                yield return new CommitedOrdersResponse()
                {
                    Entry = group.Select(t => DispozitieLivrareEntry.create(t, t.Cantitate)).OrderBy(t => t.DataDocumentBaza).ToList()
                };
            }
        }
    }
}
