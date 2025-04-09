using WordDocumentServices;

namespace WebApi.Models
{
    public class ComplaintDocument : IVisitable<int>
    {
        public DateTime Date { get; set; }
        public string LocationName { get; set; }
        public string LocationCode { get; set; }
        public List<ComplaintEntry> complaintEntries { get; set; }
        public int? TransportId { get; set; }

        public void Accept(ITemplateDocumentWriter visitor, List<int> contextItems, ContextMap context)
        {
            visitor.WriteToMainDoc(new Dictionary<string, string>()
            {
                { "date_field", context.GetOrDefault("date_field", Date.ToString("dd/MMM/yy")) },
                { "magazin_field", context.GetOrDefault("magazin_field", LocationName) },
                { "driver_name", context.GetOrDefault("driver_name", context.GetDots())  }
            });

            foreach (var complaint in complaintEntries)
            {
                complaint.Accept(visitor, contextItems, context);
            }
            if (context.ContainsKey("identity"))
            {
                using (var img = context.GenerateQrCode(context["identity"] as string, context["identityD"] as string, context.QRSIZE))
                    visitor.WriteImage(img, "identity", context.QRSIZE, context.QRSIZE);
            }
        }

        public string GetMd5(Func<string, string> getMd5)
        {
            List<string> list = new List<string>();
            foreach (var item in complaintEntries)
            {
                list.Add($"{item.Description}{item.UM}{item.Quantity}");
            }
            list.Add(LocationCode);
            list.Add(complaintEntries.Count().ToString());
            var stringToHash = string.Join("", list.Order());
            return getMd5(stringToHash.ToString());
        }
    }

    public class ComplaintEntry : IVisitable<int>
    {
        public string Description { get; set; }
        public string UM { get; set; }
        public string Quantity { get; set; }
        public string Observation { get; set; }
        public string? RefPartitionKey { get; set; }
        public string? RefRowKey { get; set; }
        public bool? CloseTask { get; set; }

        public void Accept(ITemplateDocumentWriter visitor, List<int> contextItems, ContextMap context)
        {
            visitor.WriteToTable("reclamatii", [[context.IncrementIndex().ToString(), Description, UM, Quantity, Observation]]);
        }
    }
}
