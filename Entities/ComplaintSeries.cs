namespace WebApi.Entities
{
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;

    public class ComplaintSeries : IDataKey
    {
        public ComplaintSeries()
        {
            this.Tickets = new List<Ticket>();
        }

        [Key]
        public int Id { get; set; }
        public List<Ticket> Tickets { get; set; }
        public string DataKey { get; set; }
    }
}
