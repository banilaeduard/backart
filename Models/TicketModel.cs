namespace WebApi.Models
{
    using System;
    using System.Collections.Generic;
    using DataAccess.Entities;
    public class TicketModel
    {
        public int Id { get; set; }
        public string CodeValue { get; set; }
        public string Description { get; set; }
        public bool hasAttachments { get;set;}
        public List<Attachment> Attachments { get; set; }
        public List<Attachment> ToAddAttachment { get; set; }
        public List<Attachment> ToDeleteAttachment { get; set; }
        public List<CodeLink> CodeLinks { get; set; }
        public DateTime Created { get; set; }
        public Dictionary<string, object> Tags { get; set; }

        public static TicketModel from(Ticket dbTicket, Dictionary<string, object> tags)
        {
            return new TicketModel()
            {
                Id = dbTicket.Id,
                CodeValue = dbTicket.CodeValue,
                Description = dbTicket.Description,
                hasAttachments = dbTicket.HasAttachments,
                CodeLinks = dbTicket.CodeLinks,
                Attachments = dbTicket.Attachments,
                Created = dbTicket.CreatedDate,
                Tags = tags
            };
        }

        public Ticket toDbModel()
        {
            return new Ticket()
            {
                Id = this.Id,
                CodeValue = this.CodeValue,
                Description = this.Description,
                CodeLinks = this.CodeLinks,
                Attachments = this.Attachments
            };
        }
    }
}
