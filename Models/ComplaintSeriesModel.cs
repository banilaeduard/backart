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
        public string DataKey { get; set; }
        public List<TicketModel> Tickets { get; set; }
        public string Status { get; set; }
        public string NrComanda { get; set; }

        public DateTime Created;

        [JsonConstructor]
        private ComplaintSeriesModel() { }
        private ComplaintSeriesModel(ComplaintSeries complaint)
        {
            this.Id = complaint.Id;
            this.DataKey = complaint.DataKey?.locationCode;
            this.Tickets = complaint.Tickets.Select(t => TicketModel.from(t)).ToList();
            this.Created = complaint.CreatedDate;
            this.Status = complaint.Status;
            this.NrComanda = NrComanda;
        }

        public ComplaintSeries toDbModel()
        {
            return new ComplaintSeries()
            {
                Id = this.Id,
                Tickets = this.Tickets.Select(ticket => ticket.toDbModel()).ToList(),
                Status = this.Status,
                NrComanda = this.NrComanda,
            };
        }

        public static ComplaintSeriesModel from(ComplaintSeries dbModel)
        {
            return new ComplaintSeriesModel(dbModel);
        }
    }
}
