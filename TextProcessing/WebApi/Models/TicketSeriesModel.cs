namespace WebApi.Models
{
    using System.Collections.Generic;
    using System.Text.Json.Serialization;
    using System;
    using RepositoryContract.Tickets;

    public class TicketSeriesModel
    {
        public string PartitionKey { get; set; }
        public string RowKey { get; set; }
        public List<TicketModel> Tickets { get; set; }
        public string NrComanda { get; set; }

        public DateTime? Created { get; set; }

        public string? DataKey { get; set; }

        public string? Status { get; set; }

        [JsonConstructor]
        private TicketSeriesModel() { }
        private TicketSeriesModel(TicketEntity complaint)
        {
            PartitionKey = complaint.PartitionKey;
            RowKey = complaint.RowKey;
            Tickets = [new TicketModel()
            {
                Description = complaint.Description,
                CodeValue = complaint.RowKey
            }];
            Created = complaint.CreatedDate;
            NrComanda = complaint.NrComanda;
            Status = complaint.From;
        }

        public static TicketSeriesModel from(TicketEntity dbModel)
        {
            return new TicketSeriesModel(dbModel);
        }
    }
}
