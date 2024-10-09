namespace RepositoryContract.Tasks
{
    public class TaskAction
    {
        public int Id { get; set; }
        public int TaskId { get; set; }
        public int ActionId { get; set; }
        public string Description { get; set; }
        public DateTime Created { get; set; }
    }
}
