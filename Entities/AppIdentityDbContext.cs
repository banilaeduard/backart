namespace WebApi.Entities
{
    using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore;
    public class AppIdentityDbContex : IdentityDbContext<AppIdentityUser, AppIdentityRole, string>
    {
        public AppIdentityDbContex(DbContextOptions<AppIdentityDbContex> ctxBuilder) : base(ctxBuilder)
        {

        }
    }
}