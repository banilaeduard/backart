using RepositoryContract.DataKeyLocation;
using RepositoryContract.Tasks;
using RepositoryContract.Tickets;

namespace WebApi.Models
{
    public class TaskModel
    {
        public int? Id { get; set; }
        public string Name { get; set; }
        public string Details { get; set; }
        public string LocationCode { get; set; }
        public string? LocationName { get; set; }
        public bool IsClosed { get; set; }
        public DateTime Created { get; set; }
        public IEnumerable<TicketSeriesModel>? ExternalMailReferences { get; set; }

        public TaskEntry ToTaskEntry()
        {
            var taskModel = new TaskEntry()
            {
                Created = Created,
                Details = Details,
                Id = Id ?? 0,
                Name = Name,
                LocationCode = LocationCode,
                IsClosed = IsClosed,
            };

            taskModel.ExternalReferenceEntries = ExternalMailReferences?.SelectMany(t => t.Tickets).Select(t => new ExternalReferenceEntry()
            {
                ExternalGroupId = t.ThreadId,
                PartitionKey = t.PartitionKey,
                RowKey = t.RowKey,
                TableName = nameof(TicketEntity),
                IsRemoved = false,
                Date = t.Created ?? DateTime.Now
                
            }).ToList() ?? [];

            return taskModel;
        }

        public static IEnumerable<TaskModel> From(IEnumerable<TaskEntry> tasks, IEnumerable<TicketEntity> tickets, IEnumerable<DataKeyLocationEntry> locations)
        {
            List<TaskModel> result = new();
            foreach (var task in tasks)
            {
                var mainLocation = locations.Where(t => t.LocationCode == task.LocationCode).OrderByDescending(t => t.MainLocation).FirstOrDefault();

                var taskModel = new TaskModel()
                {
                    Created = task.Created,
                    Details = task.Details,
                    Id = task.Id,
                    Name = task.Name,
                    LocationCode = task.LocationCode,
                    LocationName = mainLocation?.LocationName,
                    IsClosed = task.IsClosed,
                };
                result.Add(taskModel);

                var relatedTickets = tickets.Where(t => task.ExternalReferenceEntries?.Any(
                    er => er.TableName == nameof(TicketEntity)
                            && er.PartitionKey == t.PartitionKey && er.RowKey == t.RowKey) == true)
                    .GroupBy(T => T.ThreadId)
                    .Select(t => TicketSeriesModel.from([.. t], null))
                    .ToList();

                taskModel.ExternalMailReferences = relatedTickets ?? [];
            }
            return result;
        }
    }
}
