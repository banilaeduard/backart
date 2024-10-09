namespace RepositoryContract.Tasks
{
    public class TaskState
    {
        public int Id { get; set; }
        public int TaskId { get; set; }
        public int StateId { get; set; }
        public string Description { get; set; }
        public DateTime Created { get; set; }
        public DateTime? ValidTo { get; set; }
    }
}
