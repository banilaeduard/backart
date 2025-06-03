namespace WebApi.Models
{
    using System.Collections.Generic;
    using System.Text.Json.Serialization;
    using System;
    using RepositoryContract.Tickets;
    using RepositoryContract.Tasks;
    using RepositoryContract;
    using EntityDto.Tasks;

    public class TicketSeriesModel : TableEntityPK
    {
        public int? Id { get; set; }
        public List<TicketModel> Tickets { get; set; }
        public string? NrComanda { get; set; }

        public DateTime? Created { get; set; }

        public string? DataKey { get; set; }

        public string? Status { get; set; }

        public string? LocationPartitionKey { get; set; }

        public string? LocationRowKey { get; set; }

        [JsonConstructor]
        private TicketSeriesModel() { }
        private TicketSeriesModel(TicketEntity[] complaints, IList<ExternalReference>? externalRefs)
        {
            var map = (TicketEntity e) =>
            {
                return TicketModel.FromEntry(e, externalRefs?.FirstOrDefault(x => x.PartitionKey == e.PartitionKey && x.RowKey == e.RowKey && x.EntityType == nameof(TicketEntity)));
            };
            var complaint = complaints.MaxBy(t => t.CreatedDate)!;

            Id = map(complaint)?.Id;
            PartitionKey = complaint.PartitionKey;
            RowKey = complaint.RowKey;
            Tickets = [.. complaints.OrderByDescending(t => t.CreatedDate).Select(map)];
            Created = complaint.CreatedDate;
            NrComanda = complaint.NrComanda ?? "";
            Status = complaint.From;
            DataKey = complaint.LocationCode ?? complaint.Locations;
            LocationPartitionKey = complaint.LocationPartitionKey;
            LocationRowKey = complaint.LocationRowKey;
        }

        public static TicketSeriesModel from(TicketEntity[] dbModel, IList<ExternalReference>? externalRefs)
        {
            return new TicketSeriesModel(dbModel, externalRefs);
        }
    }
}
