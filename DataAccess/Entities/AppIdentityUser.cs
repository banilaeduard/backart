namespace DataAccess.Entities
{
    using System.Collections.Generic;
    using Microsoft.AspNetCore.Identity;
    public class AppIdentityUser : IdentityUser
    {
        public string Name { get; set; }
        public string Address { get; set; }
        public List<RefreshToken> RefreshTokens { get; set; }
        public string DataKey { get; set; }
    }
}