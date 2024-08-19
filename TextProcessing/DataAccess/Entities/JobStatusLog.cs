using System;
using System.ComponentModel.DataAnnotations;

namespace DataAccess.Entities
{
    internal class JobStatusLog : IBaseEntity, ITenant
    {
        [Key]
        public int Id { get; set; }
        public string Message { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime UpdatedDate { get; set; }
        public string TenantId { get; set; }
    }
}