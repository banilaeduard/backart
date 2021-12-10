namespace WebApi.Entities
{
    using System.ComponentModel.DataAnnotations;

    public class Image
    {
        [Key]
        public int Id { get; set; }

        public string Data { get; set; }

        public string Title { get; set; }
    }
}