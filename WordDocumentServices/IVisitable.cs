namespace WordDocumentServices
{
    public interface IVisitable<T>
    {
        public void Accept(ITemplateDocumentWriter visitor, List<T> contextItems, ContextMap context);
    }
}
