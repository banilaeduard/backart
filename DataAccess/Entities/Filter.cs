using System;
using System.ComponentModel.DataAnnotations;

namespace DataAccess.Entities
{
    public class Filter : IBaseEntity, ITenant
    {
        [Key]
        public int Id { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime UpdatedDate { get; set; }
        public string Query { get; set; }
        public string Name { get; set; }
        public string Tags { get; set; }
        public string TenantId { get; set; }
    }
}
