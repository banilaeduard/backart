namespace WebApi.Models
{
    using System.Collections.Generic;
    using DataAccess.Entities;
    using System.Linq;
    using System.Text.Json.Serialization;
    using System;

    public class ComplaintSeriesModel
    {
        public int Id { get; set; }
        public List<TicketModel> Tickets { get; set; }
        public string NrComanda { get; set; }

        public DateTime Created;

        [JsonConstructor]
        private ComplaintSeriesModel() { }
        private ComplaintSeriesModel(ComplaintSeries complaint, List<Dictionary<string, object>> tags)
        {
            Id = complaint.Id;
            Tickets = complaint.Tickets.Select(t => TicketModel.from(t, tags?.Find(solrDoc => Convert.ToInt32(solrDoc["id"]) == t.Id))).ToList();
            Created = complaint.CreatedDate;
            NrComanda = complaint.NrComanda;
        }

        public ComplaintSeries toDbModel()
        {
            return new ComplaintSeries()
            {
                Id = Id,
                Tickets = Tickets.Select(ticket => ticket.toDbModel()).ToList(),
                NrComanda = NrComanda,
            };
        }

        public static ComplaintSeriesModel from(ComplaintSeries dbModel, List<Dictionary<string, object>> tags)
        {
            return new ComplaintSeriesModel(dbModel, tags);
        }
    }
}
