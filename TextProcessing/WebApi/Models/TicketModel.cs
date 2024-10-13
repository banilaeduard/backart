namespace WebApi.Models
{
    using RepositoryContract.Tickets;
    using System;
    public class TicketModel
    {
        public string? PartitionKey { get; set; }
        public string? RowKey { get; set; }
        public string? CodeValue { get; set; }
        public string? Description { get; set; }
        public bool? hasAttachments { get; set; }
        public DateTime? Created { get; set; }
        public string? Location { get; set; }
        public string? From { get; set; }
        public string? Subject { get; set; }
        public string? OriginalBody { get; set; }
        public string? ThreadId { get; set; }

        public static TicketModel FromEntry(TicketEntity t)
        {
            return new TicketModel()
            {
                Description = t.Description,
                CodeValue = t.Subject ?? "",
                Location = t.LocationCode ?? t.Locations ?? "",
                RowKey = t.RowKey,
                PartitionKey = t.PartitionKey,
                From = t.From ?? "",
                Subject = t.Subject ?? "",
                Created = t.CreatedDate,
                OriginalBody = t.OriginalBodyPath,
                ThreadId = t.ThreadId,
            };
        }
    }
}