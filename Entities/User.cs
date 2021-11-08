namespace WebApi.Entities
{
    using System;
    using System.Text.Json.Serialization;
    using System.Collections.Generic;

    public class User
    {
        [JsonIgnore]
        public string Id { get; set; }
        public string Name { get; set; }

        public string Email { get; set; }

        public string Phone { get; set; }

        public string Address { get; set; }

        public DateTime Birth { get; set; }
        public string PasswordHash;

        [JsonIgnore]
        public List<RefreshToken> RefreshTokens { get; set; }

        public User From(User model)
        {
            this.Name = model.Name;
            this.Phone = model.Phone;
            this.Address = model.Address;
            this.Birth = model.Birth;
            return this;
        }
    }
}