namespace DataAccess.Entities
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;

    public class ComplaintSeries : IDataKey, IBaseEntity, ITenant
    {
        public ComplaintSeries()
        {
            Tickets = new List<Ticket>();
        }

        [Key]
        public int Id { get; set; }
        public List<Ticket> Tickets { get; set; }
        public string DataKey { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime UpdatedDate { get; set; }
        public string TenantId { get; set; }
    }
}
