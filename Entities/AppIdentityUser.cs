namespace WebApi.Entities
{
    using System;
    using System.Collections.Generic;
    using Microsoft.AspNetCore.Identity;
    public class AppIdentityUser : IdentityUser
    {
        public string Name { get; set; }
        public string Address { get; set; }
        public DateTime Birth { get; set; }
        public List<RefreshToken> RefreshTokens { get; set; }
    }
}