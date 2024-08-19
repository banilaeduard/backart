namespace Entities
{
    using System;
    using System.ComponentModel.DataAnnotations;

    public class Attachment
    {
        [Key]
        public int Id { get; set; }
        public string Data { get; set; }
        public string Title { get; set; }
        public int TicketId { get; set; }
        public Ticket Ticket { get; set; }
        public string Extension { get; set; }
        public string ContentType { get; set; }
        public string StorageType { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime UpdatedDate { get; set; }
    }
}