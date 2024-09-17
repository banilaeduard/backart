namespace EntityDto
{
    public class DispozitieLivrare
    {
        public DispozitieLivrare() { }

        [MapExcel(10,2)]
        public string? CodLocatie {  get; set; }
        [MapExcel(6,2)]
        public int NumarIntern { get; set; }
        [MapExcel(1)]
        public string? CodProdus { get; set; }
        [MapExcel(2)]
        public string? NumeProdus { get; set; }
        [MapExcel(5)]
        public int Cantitate { get; set; }
    }
}
