namespace WebApi.Entities
{
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    public class Ticket
    {
        [Key]
        public int Id { get; set; }
        public Code Code { get; set; }
        public string CodeValue { get; set; }
        public string Description { get; set; }
        public List<Image> Images { get; set; }
        public bool HasImages { get; set; }
    }
}