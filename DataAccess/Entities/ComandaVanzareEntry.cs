using System;
using System.ComponentModel.DataAnnotations;

namespace DataAccess.Entities
{
    public class ComandaVanzareEntry
    {
        //[Key]
        //public int Id { get; set; }
        [Key]
        public int DocId { get; set; }
        //public string DetaliiDoc { get; set; }
        //public DateTime DataDoc { get; set; }
        //public string CodLocatie { get; set; }
        //public string NumeLocatie { get; set; }
        public string NumarComanda { get; set; }
        //public string CodArticol { get; set; }
        public string NumeArticol { get; set; }
        public decimal Cantitate { get; set; }
        //public DateTime CreatedDate { get; set; }
        //public DateTime UpdatedDate { get; set; }
        //public string DataKeyId { get; set; }
        //public DataKeyLocation DataKey { get; set; }
       //public bool isDeleted { get; set; }
        //public string TenantId { get; set; }
    }
}
