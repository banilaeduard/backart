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
            foreach (var entityEntry in ChangeTracker.Entries<AppIdentityUser>().ToList())
            {
                if (entityEntry.State == EntityState.Added)
                {
                    var user = entityEntry.Entity;
                    // we sanitize the location based on our logic

                    user.DataKeyLocationId = user.UserName;

                    var prevDataKey = user.DataKeyLocation;
                    if (user.DataKeyLocation != null)
                    {
                        user.DataKeyLocation = null;
                        entityEntry.Navigation("DataKeyLocation").IsLoaded = false;
                        foreach (var dataKey in ChangeTracker.Entries<DataKeyLocation>().ToList())
                            if (dataKey.Entity == prevDataKey)
                            {
                                dataKey.State = EntityState.Detached;
                            }
                    }
                    entityEntry.Navigation("DataKeyLocation").Load();
                    if (entityEntry.Navigation("DataKeyLocation").CurrentValue == null)
                    {
                        user.DataKeyLocation = new DataKeyLocation()
                        {
                            locationCode = prevDataKey?.locationCode ?? user.UserName,
                            name = user.UserName,
                        };
                    }
                    else
                    {
                        user.DataKeyLocation.locationCode = prevDataKey?.locationCode ?? user.DataKeyLocation.locationCode;
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
