namespace RepositoryContract.DataKeyLocation
{
    public class DataKeyLocationBase
    {
        public int Id { get; set; }
        public bool MainLocation { get; set; }
        public string LocationName { get; set; }
        public string LocationCode { get; set; }
        public string TownName { get; set; }
    }
}
