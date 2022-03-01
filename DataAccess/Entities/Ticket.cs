namespace DataAccess.Entities
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    public class Ticket: IBaseEntity
    {
        [Key]
        public int Id { get; set; }
        public string CodeValue { get; set; }
        public string Description { get; set; }
        public List<Image> Images { get; set; }
        public List<CodeLink> codeLinks { get; set; }
        public bool HasImages { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime UpdatedDate { get; set; }
    }
}