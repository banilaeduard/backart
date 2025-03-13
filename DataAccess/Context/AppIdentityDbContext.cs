namespace DataAccess.Context
{
    using DataAccess.Entities;
    using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.Metadata.Internal;
    using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
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

                //var props2 = entity.ClrType.GetProperties().Where(t => (t.PropertyType == typeof(DateTimeOffset?)
                //    || t.PropertyType == typeof(DateTimeOffset)
                //) && t.CanWrite);
                //foreach (var e in props2)
                //{
                //    modelBuilder.Entity(entity.ClrType)
                //           .Property(e.Name).HasConversion(new ValueConverter<DateTimeOffset?, DateTime?>(
                //                v => v.HasValue ? v.Value.DateTime : null,
                //                v => v.HasValue ? new DateTimeOffset(v.Value) : null)
                //           );
                //}
            }

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
