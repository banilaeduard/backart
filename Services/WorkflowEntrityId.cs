namespace ServiceInterface.Storage
{
    public class WorkflowEntrityId<T>
    {
        public string Id { get; set; }
        public string Id2 { get; set; }
        public DateTime? Timestamp { get; set; }
        public T Model { get; set; }
        public WorkflowEntrityId(string Id, string Id2, T model, DateTime? time)
        {
            this.Id = Id;
            this.Id2 = Id2;
            this.Model = model;
            this.Timestamp = time;
        }
    }
}
