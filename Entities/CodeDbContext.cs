namespace WebApi.Entities
{
    using Microsoft.EntityFrameworkCore;
    public class CodeDbContext : DbContext
    {
        public CodeDbContext(DbContextOptions<CodeDbContext> ctxBuilder) : base(ctxBuilder)
        {
        }
        public DbSet<Code> Codes { get; set; }
    }
}