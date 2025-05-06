using WordDocumentServices;

namespace RepositoryServices.Models
{
    public class WorkerPriorityList : ITemplateDocumentWriter, IVisitable<Dictionary<string, int>>
    {
        public string Grouping { get; set; }
        public WorkerPriorityList(List<WorkItem> WorkItems, string grouping)
        {
            this.WorkItems = WorkItems;
            this.WorkDisplayItems = [];
            Grouping = grouping;
        }
        public List<WorkItem> WorkItems { get; set; }
        public List<ReportModel> WorkDisplayItems { get; set; }
        public void Accept(ITemplateDocumentWriter visitor, Dictionary<string, int> contextItems, ContextMap context)
        {
            foreach(var wItem in WorkItems)
            {
                wItem.Accept(visitor, contextItems, context);
            }
        }

        public void Dispose()
        {
        }

        public Stream GetStream()
        {
            return Stream.Null;
        }

        public ITemplateDocumentWriter SetTemplate(Stream stream)
        {
            return this;
        }

        public void WriteImage(Stream imagePath, string tagValue, int legnth = 100, int width = 100)
        {
        }

        public void WriteToMainDoc(Dictionary<string, string> keyValuePairs)
        {
        }

        public void WriteToTable(string tagName, string[][] values)
        {
            if (values.Length == 0)
            {
                return;
            }
        }
    }

    public class WorkItem : IVisitable<Dictionary<string, int>>
    {
        public string CodProdus { get; set; }
        public string NumeProdus { get; set; }
        public string NumarComanda { get; set; }
        public string CodLocatie { get; set; }
        public DateTime? DeliveryDate { get; set; }
        public int Cantitate { get; set; }
        public void Accept(ITemplateDocumentWriter visitor, Dictionary<string, int> contextItems, ContextMap context)
        {
            if (contextItems.ContainsKey(CodProdus)) contextItems[CodProdus] += Cantitate;
        }
    }

    public class WorkDisplayItem
    {
        public string Display { get; set; }
        public int Cantitate { get; set; }
        public int Ordere { get; set; }
    }
}
