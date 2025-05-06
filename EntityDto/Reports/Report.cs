using System.Runtime.Serialization;

namespace EntityDto.Reports
{
    [DataContract]
    public class Report : IdentityEquality<Report>, ITableEntryDto
    {
        [DataMember]
        public string PartitionKey { get; set; }
        [DataMember]
        public string RowKey { get; set; }
        [DataMember]
        public DateTimeOffset? Timestamp { get; set; }
        [DataMember]
        public string Group { get; set; }
        [DataMember]
        public int Order { get; set; }
        [DataMember]
        public string Display { get; set; }
        [DataMember]
        public string UM { get; set; }
        [DataMember]
        public string FindBy { get; set; }
        [DataMember]
        public int Level { get; set; }
        [DataMember]
        public int Id { get; set; }
        [DataMember]
        public int? Quantity { get; set; }
    }
}
