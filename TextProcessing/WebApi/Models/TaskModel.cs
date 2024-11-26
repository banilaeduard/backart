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
        public bool IsClosed { get; set; }
        public DateTime Created { get; set; }
        public required string TaskDate { get; set; }
        public LocationModel LocationModel { get; set; }
        public IEnumerable<TicketSeriesModel>? ExternalMailReferences { get; set; }
        public IEnumerable<UserUpload> UserUploads { get; set; }

        public TaskEntry ToTaskEntry()
        {
            var taskModel = new TaskEntry()
            {
                Created = Created,
                TaskDate = DateTime.SpecifyKind(DateTime.Parse(TaskDate),
                                            DateTimeKind.Utc),
                Details = Details,
                Id = Id ?? 0,
                Name = Name,
                LocationCode = LocationModel.LocationCode,
                IsClosed = IsClosed,
            };

            taskModel.ExternalReferenceEntries = ExternalMailReferences?.SelectMany(t => t.Tickets).Select(t => new ExternalReferenceEntry()
            {
                Id = t.Id,
                ExternalGroupId = t.ThreadId,
                PartitionKey = t.PartitionKey,
                RowKey = t.RowKey,
                TableName = nameof(TicketEntity),
                IsRemoved = t.IsRemoved,
                Date = t.Created ?? DateTime.Now
            }).ToList() ?? [];

            taskModel.ExternalReferenceEntries.AddRange(UserUploads.Select(u => new ExternalReferenceEntry()
            {
                PartitionKey = u.PartitionKey,
                RowKey = u.RowKey,
                TableName = nameof(AttachmentEntry),
                IsRemoved = false,
                ExternalGroupId = u.Path,
                Date = u.Created ?? DateTime.Now,
                Id = u.Id ?? 0
            }));

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
                    TaskDate = task.TaskDate.ToString("MM/dd/yyyy"),
                    Details = task.Details,
                    Id = task.Id,
                    Name = task.Name,
                    LocationModel = new LocationModel()
                    {
                        LocationCode = task.LocationCode,
                        LocationName = mainLocation?.LocationName,
                        PartitionKey = mainLocation?.PartitionKey,
                        RowKey = mainLocation?.RowKey,
                    },
                    IsClosed = task.IsClosed,
                };
                result.Add(taskModel);

                var relatedTickets = tickets.Where(t => task.ExternalReferenceEntries?.Any(
                    er => er.TableName == nameof(TicketEntity)
                            && er.PartitionKey == t.PartitionKey && er.RowKey == t.RowKey) == true)
                    .GroupBy(T => T.ThreadId)
                    .Select(t => TicketSeriesModel.from([.. t], [.. task.ExternalReferenceEntries]))
                    .ToList();

                taskModel.ExternalMailReferences = relatedTickets ?? [];
                taskModel.UserUploads = task.ExternalReferenceEntries?.Where(x => x.TableName == nameof(AttachmentEntry)).Select(a => new UserUpload()
                {
                    TableName = a.TableName,
                    Created = a.Created,
                    PartitionKey = a.PartitionKey,
                    RowKey = a.RowKey,
                    FileName = a.ExternalGroupId,
                    Path = a.ExternalGroupId,
                    Id = a.Id
                }) ?? [];
            }
            return result.OrderByDescending(t => t.TaskDate);
        }
    }
}
