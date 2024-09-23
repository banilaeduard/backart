namespace EntityDto
{
    public class StructuraCod
    {
        public StructuraCod() { }

        [MapExcel(15)]
        public string? CodEanProdusParinte { get; set; }
        [MapExcel(16)]
        public string NumeArticol { get; set; }
        [MapExcel(17)]
        public string CodColet { get; set; }
        [MapExcel(18)]
        public string NumeColet { get; set; }
        [MapExcel(19)]
        public string NumarColet { get; set; }
    }
}