﻿namespace EntityDto
{
    public class ComandaVanzare
    {
        public ComandaVanzare() { }

        [MapExcel(1, type = typeof(string))]
        public int DocId { get; set; }
        [MapExcel(2)]
        public string? DetaliiDoc { get; set; }
        [MapExcel(3)]
        public DateTime DataDoc { get; set; }
        [MapExcel(5)]
        public string NumePartener { get; set; }
        [MapExcel(6)]
        public string CodLocatie { get; set; }
        [MapExcel(7)]
        public string NumeLocatie { get; set; }
        [MapExcel(8)]
        public string NumarComanda { get; set; }
        [MapExcel(9)]
        public string CodArticol { get; set; }
        [MapExcel(10)]
        public string NumeArticol { get; set; }
        [MapExcel(11)]
        public int Cantitate { get; set; }
        [MapExcel(15)]
        public string? DetaliiLinie { get; set; }
        public bool? HasChildren { get; set; }
    }
}