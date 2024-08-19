namespace Entities
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;

    public class ComplaintSeries
    {
        public ComplaintSeries()
        {
            Tickets = new List<Ticket>();
        }

        [Key]
        public int Id { get; set; }
        public List<Ticket> Tickets { get; set; }
        public DataKeyLocation DataKey { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime UpdatedDate { get; set; }
        public string TenantId { get; set; }
        public string Status { get; set; }
        public string NrComanda { get; set; }
        public bool isDeleted { get; set; }
        public string DataKeyId { get; set; }
    }

    public static class Constants
    {
        public const string COMPLAINT_SUCCESS = "ACCEPTED";
        public const string COMPLAINT_DAFT = "DRAFT";
        public const string COMPLAINT_REJECT = "REJECTED";
    }
}
