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
        public DateTime Created { get; set; }
        public IEnumerable<TicketSeriesModel> ExternalMailReferences { get; set; }

        public TaskEntry ToTaskEntry()
        {
            var taskModel = new TaskEntry()
            {
                Created = Created,
                Details = Details,
                Id = Id ?? 0,
                Name = Name,
                LocationCode = LocationCode
            };

            taskModel.ExternalReferenceEntries = ExternalMailReferences.SelectMany(t => t.Tickets).Select(t => new ExternalReferenceEntry()
            {
                ExternalGroupId = t.ThreadId,
                PartitionKey = t.PartitionKey,
                RowKey = t.RowKey,
                TableReferenceName = typeof(TicketEntity).Name,
                IsRemoved = false
            }).ToList();

            return taskModel;
        }

        public static IEnumerable<TaskModel> From(IEnumerable<TaskEntry> tasks, IEnumerable<TicketEntity> tickets, IEnumerable<DataKeyLocationEntry> locations)
        {
            List<TaskModel> result = new();
            foreach (var task in tasks)
            {
                var taskModel = new TaskModel()
                {
                    Created = task.Created,
                    Details = task.Details,
                    Id = task.Id,
                    Name = task.Name,
                    LocationCode = task.LocationCode
                };
                result.Add(taskModel);

                var relatedTickets = tickets.Where(t => task.ExternalReferenceEntries?.Any(
                    er => er.TableReferenceName == nameof(TicketEntity)
                            && er.PartitionKey == t.PartitionKey && er.RowKey == t.RowKey) == true)
                    .GroupBy(T => T.ThreadId)
                    .Select(t => TicketSeriesModel.from([.. t]))
                    .ToList();

                taskModel.ExternalMailReferences = relatedTickets ?? [];
            }
            return result;
        }
    }
}
