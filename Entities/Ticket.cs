namespace Entities
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    public class Ticket
    {
        [Key]
        public int Id { get; set; }
        public string CodeValue { get; set; }
        public string Description { get; set; }
        public List<Attachment> Attachments { get; set; }
        public List<CodeLink> CodeLinks { get; set; }
        public bool HasAttachments { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime UpdatedDate { get; set; }
        public int ComplaintId { get; set; }
        public ComplaintSeries Complaint { get; set; }
        public bool isDeleted { get; set; }
    }
}