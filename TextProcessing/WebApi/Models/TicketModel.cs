namespace WebApi.Models
{
    using System;
    public class TicketModel
    {
        public string PartitionKey { get; set; }
        public string RowKey { get; set; }
        public string? CodeValue { get; set; }
        public string? Description { get; set; }
        public bool? hasAttachments { get; set; }
        public DateTime? Created { get; set; }
        public string? Location { get; set; }
        public string? From { get; set; }
        public string? Subject { get; set; }
        public string? OriginalBody { get; set; }
    }
}