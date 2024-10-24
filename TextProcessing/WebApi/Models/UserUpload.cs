namespace WebApi.Models
{
    public class UserUpload: TableEntryModel
    {
        public int? Id { get; set; }
        public string FileName { get; set; }
        public string Path { get; set; }
        public string? Type { get; set; }
        public DateTime? Created {  get; set; }
    }
}
