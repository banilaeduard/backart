using RepositoryContract.CommitedOrders;
using RepositoryContract.DataKeyLocation;
using RepositoryContract.ProductCodes;
using RepositoryContract.Tasks;
using RepositoryContract.Tickets;

namespace WebApi.Models
{
    public class CommitedOrdersResponse
    {
        public List<CommitedOrderModel> Entry { get; set; }
        public List<TicketSeriesModel> Tickets { get; set; }
        public List<TaskModel> Tasks { get; set; }

        public string NumarIntern { get; set; }
        public string NumeLocatie { get; set; }
        public string CodLocatie { get; set; }
        public DateTime DataDocument { get; set; }
        public DateTime? DataDocumentBaza { get; set; }
        public int Weight { get; set; }
        public string DetaliiComenzi { get; set; }
        public bool Livrata { get; set; }
        public int? NumarAviz { get; set; }
        public DateTime? DataAviz { get; set; }
        public string StatusName { get; set; }
        public bool HasReports { get; set; }

        public static IEnumerable<CommitedOrdersResponse> From(IList<CommitedOrderEntry> entries, IList<TicketEntity> tickets, IList<DataKeyLocationEntry> synonimLocations,
            IList<TaskEntry> tasks, IList<ProductCodeStatsEntry> productLinkWeights, IList<ProductStatsEntry> weights)
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

                var response = new CommitedOrdersResponse()
                {
                    Entry = group.Select(t => CommitedOrderModel.create(t, t.Cantitate, int.Parse(weights.FirstOrDefault(w =>
                    {
                        var pw = productLinkWeights.FirstOrDefault(x => x.PartitionKey == t.CodProdus);
                        return w.RowKey == pw?.StatsRowKey && w.PartitionKey == pw?.StatsPartitionKey;
                    })?.PropertyValue ?? "0") * t.Cantitate)).OrderBy(t => t.DataDocumentBaza).ToList(),
                    Tickets = [.. groupTickets.GroupBy(t => t.ThreadId).Select(grp => TicketSeriesModel.from([.. grp], externalRefs)).Where(t => t.Tickets?.Any(x => x.Id.HasValue) == false)],
                    Tasks = [.. TaskModel.From(groupedTasks, groupTickets, synonimLocations)],
                    DetaliiComenzi = string.Join("; ", group.Select(t => t.NumarComanda).Distinct()),
                    DataAviz = sample.DataAviz,
                    DataDocument = sample.DataDocument,
                    DataDocumentBaza = sample.DataDocumentBaza,
                    Livrata = sample.Livrata,
                    NumarAviz = sample.NumarAviz,
                    NumarIntern = sample.NumarIntern,
                    NumeLocatie = sample.NumeLocatie,
                    StatusName = sample.StatusName,
                    CodLocatie = sample.CodLocatie,
                };
                response.HasReports = response.Tasks.Any() || response.Tickets.Any();
                response.Weight = response.Entry.Sum(x => x.Greutate ?? 0);

                yield return response;
            }
        }
    }
}