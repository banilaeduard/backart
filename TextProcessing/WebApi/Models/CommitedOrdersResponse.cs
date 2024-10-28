using RepositoryContract.CommitedOrders;
using RepositoryContract.DataKeyLocation;
using RepositoryContract.Tasks;
using RepositoryContract.Tickets;

namespace WebApi.Models
{
    public class CommitedOrdersResponse
    {
        public List<DispozitieLivrareEntry> Entry { get; set; }
        public List<TicketSeriesModel> Tickets { get; set; }
        public List<TaskModel> Tasks { get; set; }

        public static IEnumerable<CommitedOrdersResponse> From(IList<DispozitieLivrareEntry> entries, IList<TicketEntity> tickets, IList<DataKeyLocationEntry> synonimLocations, IList<TaskEntry> tasks)
        {
            var externalRefs = tasks.SelectMany(t => t.ExternalReferenceEntries).DistinctBy(t => new { t.PartitionKey, t.RowKey }).ToList();

            foreach (var ticket in tickets)
            {
                if (string.IsNullOrEmpty(ticket.LocationCode))
                {
                    ticket.LocationCode = synonimLocations.FirstOrDefault(t => t.PartitionKey == ticket.LocationPartitionKey && t.RowKey == ticket.LocationRowKey)?.LocationCode ?? "";
                }
                else if (string.IsNullOrEmpty(ticket.LocationPartitionKey) || string.IsNullOrEmpty(ticket.LocationRowKey))
                {
                    var mainLocation = synonimLocations.Where(t => t.LocationCode == ticket.LocationCode).OrderByDescending(t => t.MainLocation).FirstOrDefault();
                    if (mainLocation != null)
                    {
                        ticket.LocationPartitionKey = mainLocation.PartitionKey;
                        ticket.LocationRowKey = mainLocation.RowKey;
                    }
                }
            }

            foreach (var group in entries.GroupBy(t => t.NumarIntern).OrderByDescending(t => t.Key))
            {
                var sample = group.First();
                var groupTickets = tickets.Where(t => t.LocationCode == sample.CodLocatie);
                var groupedTasks = tasks.Where(t => t.LocationCode == sample.CodLocatie).Where(t => !t.IsClosed);

                yield return new CommitedOrdersResponse()
                {
                    Entry = group.Select(t => DispozitieLivrareEntry.create(t, t.Cantitate)).OrderBy(t => t.DataDocumentBaza).ToList(),
                    Tickets = [.. groupTickets.GroupBy(t => t.ThreadId).Select(grp => TicketSeriesModel.from([.. grp], externalRefs)).Where(t => t.Tickets?.Any(x => x.Id.HasValue) == false)],
                    Tasks = [.. TaskModel.From(groupedTasks, groupTickets, synonimLocations)]
                };
            }
        }
    }
}
