using RepositoryServices.Models;
using System.Runtime.Serialization;

namespace WorkLoadService
{
    [DataContract]
    public class Items
    {
        public Items() { this.NoChange = true; }
        public Items(List<WorkerPriorityList> workerItems, List<WorkerPriorityList> orderItems)
        {
            this.OrderItems = orderItems;
            this.WorkerItems = workerItems;
        }
        [DataMember]
        public bool NoChange { get; set; }
        [DataMember]
        public string SVC { get; set; }
        [DataMember]
        public List<WorkerPriorityList> WorkerItems { get; private set; }
        [DataMember]
        public List<WorkerPriorityList> OrderItems { get; private set; }
    }
}
