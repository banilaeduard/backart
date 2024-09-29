namespace WebApi.Models
{
    using System;
    public class TicketModel
    {
        public int Id { get; set; }
        public string CodeValue { get; set; }
        public string Description { get; set; }
        public bool hasAttachments { get; set; }
        public DateTime Created { get; set; }
    }
}