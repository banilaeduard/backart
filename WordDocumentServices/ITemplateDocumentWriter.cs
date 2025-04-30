namespace WordDocumentServices
{
    public interface ITemplateDocumentWriter : IDisposable
    {
        ITemplateDocumentWriter SetTemplate(Stream stream);
        void WriteToTable(string tagName, string[][] values);
        void WriteToMainDoc(Dictionary<string, string> keyValuePairs);
        void WriteImage(Stream imagePath, string tagValue, int legnth = 100, int width = 100);
        Stream GetStream();
    }
}
