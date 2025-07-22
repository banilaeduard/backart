namespace EntityDto.Config
{
    public class ClientPriceList
    {
        [MapExcel(1)]
        public string CodProdusClient { get; set; }
        [MapExcel(3)]
        public decimal PretClient { get; set; }
    }
}
