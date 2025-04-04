namespace WordDocumentServices
{
    public interface ITemplateDocumentWriter : IDisposable
    {
        ITemplateDocumentWriter SetTemplate(string templatePath);
        void WriteToTable(string tagName, string[][] values);
        void WriteToMainDoc(Dictionary<string, string> keyValuePairs);
        void WriteImage(Stream imagePath, string tagValue);
        Stream GetStream();
    }
}
