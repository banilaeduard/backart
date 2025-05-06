using RepositoryServices.Models;

namespace WorkLoadService
{
    public class Items
    {
        public Items() { this.NoChange = true; }
        public Items(List<WorkerPriorityList> workerItems, List<WorkerPriorityList> orderItems)
        {
            this.OrderItems = orderItems;
            this.WorkerItems = workerItems;
        }
        public bool NoChange { get; set; }
        public string SVC { get; set; }
        public List<WorkerPriorityList> WorkerItems { get; private set; }
        public List<WorkerPriorityList> OrderItems { get; private set; }
    }
}
