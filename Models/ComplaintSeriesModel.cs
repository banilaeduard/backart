namespace WebApi.Models
{
    using System.Collections.Generic;
    using WebApi.Entities;
    using System.Linq;
    using System.Text.Json.Serialization;

    public class ComplaintSeriesModel
    {
        public int Id { get; set; }
        public string DataKey { get; set; }
        public List<TicketModel> Tickets { get; set; }

        [JsonConstructor]
        private ComplaintSeriesModel() { }
        private ComplaintSeriesModel(ComplaintSeries complaint)
        {
            this.Id = complaint.Id;
            this.DataKey = complaint.DataKey;
            this.Tickets = complaint.Tickets.Select(t => TicketModel.from(t)).ToList();
        }

        public ComplaintSeries toDbModel()
        {
            return new ComplaintSeries()
            {
                Id = this.Id,
                DataKey = this.DataKey,
                Tickets = this.Tickets.Select(ticket => ticket.toDbModel()).ToList()
            };
        }

        public static ComplaintSeriesModel from(ComplaintSeries dbModel)
        {
            return new ComplaintSeriesModel(dbModel);
        }
    }
}
