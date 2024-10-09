namespace WebApi.Models
{
    using System.Collections.Generic;
    using System.Text.Json.Serialization;
    using System;
    using RepositoryContract.Tickets;

    public class TicketSeriesModel
    {
        public string? PartitionKey { get; set; }
        public string? RowKey { get; set; }
        public List<TicketModel> Tickets { get; set; }
        public string? NrComanda { get; set; }

        public DateTime? Created { get; set; }

        public string? DataKey { get; set; }

        public string? Status { get; set; }

        public string? LocationPartitionKey { get; set; }

        public string? LocationRowKey { get; set; }

        [JsonConstructor]
        private TicketSeriesModel() { }
        private TicketSeriesModel(TicketEntity[] complaints)
        {
            var complaint = complaints.MaxBy(t => t.CreatedDate);

            PartitionKey = complaint.PartitionKey;
            RowKey = complaint.RowKey;
            Tickets = [..complaints.OrderByDescending(t => t.CreatedDate).Select(t => new TicketModel()
            {
                Description = t.Description,
                CodeValue = t.Subject ?? "",
                Location = t.LocationCode ?? t.Locations ?? "",
                RowKey = t.RowKey,
                PartitionKey = t.PartitionKey,
                From = t.From ?? "",
                Subject = t.Subject ?? "",
                Created = t.CreatedDate,
                OriginalBody = t.OriginalBodyPath
            })];
            Created = complaint.CreatedDate;
            NrComanda = complaint.NrComanda ?? "";
            Status = complaint.From;
            DataKey = complaint.LocationCode ?? complaint.Locations;
            LocationPartitionKey = complaint.LocationPartitionKey;
            LocationRowKey = complaint.LocationRowKey;
        }

        public static TicketSeriesModel from(TicketEntity[] dbModel)
        {
            return new TicketSeriesModel(dbModel);
        }
    }
}
