namespace WebApi.Entities
{
    using Microsoft.EntityFrameworkCore;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    public class CodeDbContext : DbContext
    {
        public CodeDbContext(DbContextOptions<CodeDbContext> ctxBuilder) : base(ctxBuilder)
        {
        }
        public DbSet<Code> Codes { get; set; }

/*        public override int SaveChanges(bool acceptAllChangesOnSuccess)
        {
            this.DeleteHierarchy(ChangeTracker);
            return base.SaveChanges(acceptAllChangesOnSuccess);
        }

        public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
        {
            this.DeleteHierarchy(ChangeTracker);
            return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        }

        private void DeleteHierarchy(Microsoft.EntityFrameworkCore.ChangeTracking.ChangeTracker ChangeTracker)
        {
            var deleted = ChangeTracker.Entries().Where(item => item.State == EntityState.Deleted && item.Entity is Code);
            //ChangeTracker.Entries().Prepend();
        }*/
    }
}