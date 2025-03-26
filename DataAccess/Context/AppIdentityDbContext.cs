namespace DataAccess.Context
{
    using DataAccess.Entities;
    using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore;
    using System;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    public class AppIdentityDbContext : IdentityDbContext<AppIdentityUser, AppIdentityRole, string>
    {
        public AppIdentityDbContext(DbContextOptions<AppIdentityDbContext> ctxBuilder) : base(ctxBuilder)
        {
        }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            foreach (var entity in modelBuilder.Model.GetEntityTypes())
            {
                var props = entity.ClrType.GetProperties().Where(t => t.PropertyType == typeof(bool) && t.CanWrite);
                foreach (var e in props)
                {
                    modelBuilder.Entity(entity.ClrType)
                           .Property(e.Name).HasConversion<System.Int16>();
                }
            }
#if DEBUG
            modelBuilder.Entity<AppIdentityUser>()
                    .Property(o => o.LockoutEnd)
                    .HasConversion(
                        v => v.HasValue ? v.Value.UtcDateTime : (DateTime?)null,
                        v => v.HasValue ? new DateTimeOffset(v.Value) : null
                    );
#endif

            base.OnModelCreating(modelBuilder);
        }

        public override int SaveChanges(bool acceptAllChangesOnSuccess)
        {
            return base.SaveChanges(acceptAllChangesOnSuccess);
        }

        public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
        {
            return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        }
    }
}