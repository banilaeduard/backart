using System.ComponentModel.DataAnnotations;

namespace DataAccess.Entities
{
    public class MailSourceConfig
    {
        [Key]
        public int Id { get; set; }
        public required string From { get; set; }
        public required string Folders { get; set; }
        public required string User { get; set; }
        public required string Password { get; set; }
        public required int DaysBefore { get; set; }
    }
}
