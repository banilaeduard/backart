namespace WebApi.Models
{
    using System.Collections.Generic;
    using System.Text.Json.Serialization;
    using System;
    using RepositoryContract.Tickets;
    using RepositoryContract.Tasks;

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

        public bool HasTasks { get; set; }

        [JsonConstructor]
        private TicketSeriesModel() { }
        private TicketSeriesModel(TicketEntity[] complaints, IList<ExternalReferenceEntry>? externalRefs)
        {
            var complaint = complaints.MaxBy(t => t.CreatedDate);

            PartitionKey = complaint.PartitionKey;
            RowKey = complaint.RowKey;
            Tickets = [..complaints.OrderByDescending(t => t.CreatedDate).Select(TicketModel.FromEntry)];
            Created = complaint.CreatedDate;
            NrComanda = complaint.NrComanda ?? "";
            Status = complaint.From;
            DataKey = complaint.LocationCode ?? complaint.Locations;
            LocationPartitionKey = complaint.LocationPartitionKey;
            LocationRowKey = complaint.LocationRowKey;
            HasTasks = externalRefs?.Any(t => complaints.Any(c => t.TableName == nameof(TicketEntity) && t.PartitionKey == c.PartitionKey && t.RowKey == c.RowKey)) == true;
        }

        public static TicketSeriesModel from(TicketEntity[] dbModel, IList<ExternalReferenceEntry>? externalRefs)
        {
            return new TicketSeriesModel(dbModel, externalRefs);
        }
    }
}
