namespace DataAccess.Entities
{
    using System.ComponentModel.DataAnnotations;

    public class DataKeyLocation
    {
        [Key]
        public string Id { get; set; }
        public string name { get; set; }
        public string locationCode { get; set; }
    }
}
