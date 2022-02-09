namespace WebApi.Entities
{
    using Microsoft.EntityFrameworkCore;
    public class CodeDbContext : DbContext
    {
        public CodeDbContext(DbContextOptions<CodeDbContext> ctxBuilder) : base(ctxBuilder)
        {
        }
        public DbSet<CodeLink> Codes { get; set; }
        public DbSet<CodeAttribute> CodeAttribute { get; set; }
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
        }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {

            modelBuilder.Entity<CodeAttribute>()
                .HasKey(t => new { t.Tag, t.InnerValue })
                .HasName("Id");
        }
    }
}