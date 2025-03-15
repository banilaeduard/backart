namespace ServiceInterface.Storage
{
    public interface IWorkflowTrigger
    {
        Task Trigger<T>(string workflowName, T model) where T : class, new();
        Task<List<WorkflowEntrityId<T>>> GetWork<T>(string workflowName) where T : class, new();
        Task ClearWork<T>(string workflowName, WorkflowEntrityId<T>[] workflowIds);
    }
}
