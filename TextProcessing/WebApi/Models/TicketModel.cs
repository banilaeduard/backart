namespace WebApi.Models
{
    using RepositoryContract.Tickets;
    using System;
    public class TicketModel
    {
        public int? Id { get; set; }
        public string? PartitionKey { get; set; }
        public string? BodyPath { get; set; }
        public string? EmlPath { get; set; }
        public string? RowKey { get; set; }
        public string? CodeValue { get; set; }
        public string? Description { get; set; }
        public DateTime? Created { get; set; }
        public string? Location { get; set; }
        public string? From { get; set; }
        public string? Subject { get; set; }
        public string? ThreadId { get; set; }
        public bool HasAttachments { get; set; }

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
                ThreadId = t.ThreadId,
                HasAttachments = t.HasAttachments,
            };
        }
    }
}