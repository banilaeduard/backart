namespace WordDocumentServices
{
    public interface IVisitable<T>
    {
        public void Accept(ITemplateDocumentWriter visitor, T contextItems, ContextMap context);
    }
}
