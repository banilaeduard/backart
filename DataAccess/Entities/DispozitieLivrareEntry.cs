﻿using System;
using System.ComponentModel.DataAnnotations;

namespace DataAccess.Entities
{
    public class DispozitieLivrareEntry
    {
        [Key]
        public Guid InternalId { get; set; }
        public DateTime DataDocument { get; set; }
        public string? CodLocatie { get; set; }
        public string? NumeLocatie { get; set; }
        public int NumarIntern { get; set; }
        public string DetaliiLinie { get; set; }
        public string DetaliiDoc { get; set; }
        public string CodProdus { get; set; }
        public string NumeProdus { get; set; }
        public int Cantitate { get; set; }
        public string? NumeCodificare { get; set; }
        public string CodEan { get; set; }
        public string NumarComanda { get; set; }
        public string StatusName { get; set; }
        public DateTime? DataDocumentBaza { get; set; }
        public bool Livrata { get; set; }
    }
}
