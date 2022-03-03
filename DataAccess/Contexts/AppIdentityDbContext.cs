namespace DataAccess.Context
{
    using DataAccess.Entities;
    using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.ChangeTracking;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    public class AppIdentityDbContext : IdentityDbContext<AppIdentityUser, AppIdentityRole, string>
    {
        public DbSet<DataKeyLocation> DataKeyLocation { get; set; }
        public AppIdentityDbContext(DbContextOptions<AppIdentityDbContext> ctxBuilder) : base(ctxBuilder)
        {
        }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<AppIdentityUser>()
                .HasOne(t => t.DataKeyLocation)
                .WithMany()
                .HasForeignKey(t => t.DataKeyLocationId)
                .HasPrincipalKey(t => t.name)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<AppIdentityUser>()
                .Navigation(t => t.DataKeyLocation)
                .AutoInclude();

            base.OnModelCreating(modelBuilder);
        }
        public override int SaveChanges(bool acceptAllChangesOnSuccess)
        {
            addDataContext(ChangeTracker);
            return base.SaveChanges(acceptAllChangesOnSuccess);
        }

        public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
        {
            addDataContext(ChangeTracker);
            return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        }

        private void addDataContext(ChangeTracker ChangeTracker)
        {
            foreach (var entityEntry in ChangeTracker.Entries().ToList())
            {
                if (entityEntry.Entity is AppIdentityUser user)
                {
                    if (entityEntry.State == EntityState.Added)
                    {
                        user.DataKeyLocationId = user.UserName;
                        entityEntry.Navigation("DataKeyLocation").Load();
                        if (entityEntry.Navigation("DataKeyLocation").CurrentValue == null)
                        {
                            user.DataKeyLocation = new DataKeyLocation()
                            {
                                locationCode = user.UserName,
                                name = user.UserName,
                            };
                        }
                    }
                    else
                    {
                        entityEntry.Property("DataKeyLocationId").IsModified = false;
                    }
                }
            }
        }
    }
}
