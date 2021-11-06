namespace WebApi.Entities
{
    using System.Text.Json.Serialization;
    using System.Collections.Generic;
    using System.Runtime.Serialization;

    [DataContract]
    public class User
    {
        public int Id { get; set; }
        [DataMember]
        public string Name { get; set; }
        [DataMember]
        public string Email { get; set; }
        [DataMember]
        public string Phone { get; set; }
        [DataMember]
        public string Address { get; set; }
        public string Password { get; set; }

        [JsonIgnore]
        public List<RefreshToken> RefreshTokens { get; set; }
    }
}