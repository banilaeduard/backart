namespace WebApi.Models
{
    public class TransportModel
    {
        public int? Id { get; set; }
        public string Description { get; set; }
        public string? Description2 { get; set; }
        public string? DriverName { get; set; }
        public string? CarPlateNumber { get; set; }
        public int Distance { get; set; }
        public int FuelConsumption { get; set; }
        public string CurrentStatus { get; set; }
        public string? ExternalItemId { get; set; }
        public DateTime? Created { get; set; }
        public DateTime? Delivered { get; set; }
        public List<TransportItemModel> TransportItems { get; set; }
        public List<UserUpload>? UserUploads { get; set; }
    }
}
