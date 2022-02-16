namespace WebApi.Models
{
    using System.Collections.Generic;
    using WebApi.Entities;

    public class TicketModel
    {
        public int Id { get; set; }
        public string CodeValue { get; set; }
        public string Description { get; set; }
        public bool hasImages { get;set;}
        public List<Image> Images { get; set; }
        public List<Image> ToAddImages { get; set; }
        public List<Image> ToDeleteImages { get; set; }
        public List<CodeLink> CodeLinks { get; set; }

        public static TicketModel from(Ticket dbTicket)
        {
            return new TicketModel()
            {
                Id = dbTicket.Id,
                CodeValue = dbTicket.CodeValue,
                Description = dbTicket.Description,
                hasImages = dbTicket.HasImages,
                CodeLinks = dbTicket.codeLinks,
                Images = dbTicket.Images
            };
        }

        public Ticket toDbModel()
        {
            return new Ticket()
            {
                Id = this.Id,
                CodeValue = this.CodeValue,
                Description = this.Description,
                codeLinks = this.CodeLinks,
                Images = this.Images
            };
        }
    }
}
