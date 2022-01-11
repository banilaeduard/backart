namespace WebApi.Models
{
    using System.Collections.Generic;
    using WebApi.Entities;
    using System.Linq;
    public class ComplaintSeriesModel
    {
        public int Id { get; set; }
        public string DataKey { get; set; }
        public List<TicketModel> Tickets { get; set; }
        public List<TicketModel> ToAddTickets { get; set; }
        public List<TicketModel> ToRemoveTickets { get; set; }
        private ComplaintSeriesModel(ComplaintSeries complaint, bool hasImagesLoaded)
        {
            this.Id = complaint.Id;
            this.DataKey = complaint.DataKey;
            this.Tickets = complaint.Tickets.Select(t => TicketModel.fromDbModel(t, hasImagesLoaded)).ToList();
            this.ImagesLoaded = hasImagesLoaded;
        }

        public bool ImagesLoaded { get; set; }

        public static ComplaintSeriesModel from(ComplaintSeries dbModel, bool hasImagesLoaded)
        {
            return new ComplaintSeriesModel(dbModel, hasImagesLoaded);
        }
    }
}
