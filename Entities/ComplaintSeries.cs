namespace WebApi.Entities
{
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;

    public class ComplaintSeries : IDataKey
    {
        [Key]
        public int Id { get; set; }
        public List<Ticket> Tickets { get; set; }
        public string DataKey { get; set; }
    }
}
