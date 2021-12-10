namespace WebApi.Entities
{
    using System.ComponentModel.DataAnnotations;
    public class Code
    {
        [Key]
        public int Id { get; set; }
        public string Display { get; set; }
        public string Value { get; set; }
    }
}