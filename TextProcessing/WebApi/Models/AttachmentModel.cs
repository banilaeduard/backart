namespace WebApi.Models
{
    public class AttachmentModel: TableEntryModel
    {
        public string Data { get; set; }
        public string Title {get;set;}
        public string ContentType {get;set;}
        public string RefKey { get; set; }
        public string RefPartition { get; set; }
    }
}
