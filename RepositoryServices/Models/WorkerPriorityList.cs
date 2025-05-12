using System.Runtime.Serialization;
using WordDocumentServices;

namespace RepositoryServices.Models
{
    [DataContract]
    public class WorkerPriorityList : ITemplateDocumentWriter, IVisitable<Dictionary<string, int>>
    {
        [DataMember]
        public string Grouping { get; set; }
        public WorkerPriorityList(List<WorkItem> WorkItems, string grouping)
        {
            this.WorkItems = WorkItems;
            this.WorkDisplayItems = [];
            Grouping = grouping;
        }
        [DataMember]
        public List<WorkItem> WorkItems { get; set; }
        [DataMember]
        public List<ReportModel> WorkDisplayItems { get; set; }
        public void Accept(ITemplateDocumentWriter visitor, Dictionary<string, int> contextItems, ContextMap context)
        {
            foreach (var wItem in WorkItems)
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
    [DataContract]
    public class WorkItem : IVisitable<Dictionary<string, int>>
    {
        [DataMember]
        public string CodProdus { get; set; }
        [DataMember]
        public string NumeProdus { get; set; }
        [DataMember]
        public string NumarComanda { get; set; }
        [DataMember]
        public string CodLocatie { get; set; }
        [DataMember]
        public DateTime? DeliveryDate { get; set; }
        [DataMember]
        public string DocId { get; set; }
        [DataMember]
        public int Cantitate { get; set; }
        [DataMember]
        public int CantRemoved { get; set; }
        [DataMember]
        public int Hash { get; set; }
        [DataMember]
        public string Detalii { get; set; }
        public void Accept(ITemplateDocumentWriter visitor, Dictionary<string, int> contextItems, ContextMap context)
        {
            if (contextItems.ContainsKey(CodProdus)) contextItems[CodProdus] += Cantitate;
        }
    }
    [DataContract]
    public class WorkDisplayItem
    {
        [DataMember]
        public string Display { get; set; }
        [DataMember]
        public int Cantitate { get; set; }
        [DataMember]
        public int Ordere { get; set; }
    }
}
