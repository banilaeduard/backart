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

        public DateTime? Created { get; set; }

        public string? DataKey { get; set; }

        public string? Status { get; set; }

        [JsonConstructor]
        private ComplaintSeriesModel() { }
        private ComplaintSeriesModel(ComplaintSeries complaint)
        {
            Id = complaint.Id;
            Tickets = complaint.Tickets.Select(TicketModel.from).ToList();
            Created = complaint.CreatedDate;
            NrComanda = complaint.NrComanda;
            DataKey = string.Format("{0} - {1}", complaint.DataKey.name, complaint.DataKey.locationCode);
            Status = complaint.Status;
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

        public ComplaintSeries toDbModel(ComplaintSeries series)
        {
            series.Tickets = Tickets.Select(t => t.toDbModel()).ToList();
            series.NrComanda = NrComanda;
            return series;
        }

        public static ComplaintSeriesModel from(ComplaintSeries dbModel)
        {
            return new ComplaintSeriesModel(dbModel);
        }
    }
}
