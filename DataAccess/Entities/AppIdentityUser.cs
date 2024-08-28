namespace DataAccess.Entities
{
    using System.Collections.Generic;
    using Microsoft.AspNetCore.Identity;
    public class AppIdentityUser : IdentityUser
    {
        public AppIdentityUser()
        {
            RefreshTokens = [];
        }
        public string Name { get; set; }
        public string Address { get; set; }
        public List<RefreshToken> RefreshTokens { get; set; }
        public DataKeyLocation DataKeyLocation { get; set; }
        public string DataKeyLocationId { get; set; }
        public string Tenant { get; set; }
    }
}
