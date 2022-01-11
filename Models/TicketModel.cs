namespace WebApi.Models
{
    using System.Collections.Generic;
    using WebApi.Entities;

    public class TicketModel
    {
        public int Id { get; set; }
        public string CodeValue { get; set; }
        public string Description { get; set; }
        public bool isLoaded {get;set;}
        public List<Image> Images { get; set; }
        public List<Image> ToAddImages { get; set; }
        public List<Image> ToRemoveImages { get; set; }
        public List<CodeLink> CodeLinks { get; set; }
        public List<CodeLink> ToAddCodeLinks { get; set; }
        public List<CodeLink> ToRemoveCodeLinks { get; set; }

        public static TicketModel fromDbModel(Ticket dbTicket, bool hasImagesLoaded)
        {
            return new TicketModel()
            {
                Id = dbTicket.Id,
                CodeValue = dbTicket.CodeValue,
                Description = dbTicket.Description,
                isLoaded = hasImagesLoaded,
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
                Images = this.Images,
                HasImages = this.Images?.Count > 0
            };
        }
    }
}
