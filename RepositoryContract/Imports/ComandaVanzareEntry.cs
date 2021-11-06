using System.ComponentModel.DataAnnotations;

namespace RepositoryContract.Imports
{
    public class ComandaVanzareEntry
    {
        [Key]
        public Guid InternalId { get; set; }
        public int DocId { get; set; }
        public string DetaliiDoc { get; set; }
        public DateTime DataDoc { get; set; }
        public string NumePartener { get; set; }
        public string CodLocatie { get; set; }
        public string NumeLocatie { get; set; }
        public string NumarComanda { get; set; }
        public string CodArticol { get; set; }
        public string NumeArticol { get; set; }
        public int CantitateTarget { get; set; }
        public int Cantitate { get; set; }
        public string DetaliiLinie { get; set; }
        public string StatusName { get; set; }
    }
}
