namespace WebApi.Entities
{
    using Microsoft.AspNetCore.Http;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.ChangeTracking;

    public class CodeDbContext : BaseContext
    {
        public CodeDbContext(DbContextOptions<CodeDbContext> ctxBuilder, IHttpContextAccessor httpContextAccessor) : 
            base(ctxBuilder, httpContextAccessor)
        {
        }
        public DbSet<CodeLink> Codes { get; set; }
        public DbSet<CodeAttribute> CodeAttribute { get; set; }

        protected override void BeforeSave(EntityEntry entityEntry)
        {
            // no op
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<CodeAttribute>()
                .HasKey(t => new { t.Tag, t.InnerValue })
                .HasName("Id");

            base.OnModelCreating(modelBuilder);
        }
    }
}