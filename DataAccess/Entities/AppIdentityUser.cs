namespace DataAccess.Entities
{
    using Microsoft.AspNetCore.Identity;
    public class AppIdentityUser : IdentityUser
    {
        public AppIdentityUser()
        {
        }
        public string Name { get; set; }
        public string Address { get; set; }
        public string DataKeyLocationId { get; set; }
        public string Tenant { get; set; }
    }
}
