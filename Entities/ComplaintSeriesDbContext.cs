namespace WebApi.Entities
{
    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.Metadata.Builders;
    using Microsoft.AspNetCore.Http;

    using System.Linq;
    using System.Security.Claims;
    using System.Threading.Tasks;
    using System.Threading;
    public class ComplaintSeriesDbContext : DbContext
    {
        private IHttpContextAccessor httpContextAccessor;
        public ComplaintSeriesDbContext(DbContextOptions<ComplaintSeriesDbContext> ctxBuilder,
            IHttpContextAccessor httpContextAccessor) : base(ctxBuilder)
        {
            this.httpContextAccessor = httpContextAccessor;
        }
        public DbSet<ComplaintSeries> Complaints { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            AddHierarchicalQueryFilter(modelBuilder.Entity<ComplaintSeries>(), this);
            base.OnModelCreating(modelBuilder);
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {

        }

        public override int SaveChanges(bool acceptAllChangesOnSuccess)
        {
            if (httpContextAccessor.HttpContext.User.FindFirst(ClaimTypes.Role)?.Value != "admin")
            {
                foreach (var entityEntry in ChangeTracker.Entries().Where(e => e.State == EntityState.Added))
                {
                    if (entityEntry.Entity is IDataKey dataKey)
                        dataKey.DataKey = httpContextAccessor.HttpContext.User.FindFirst(ClaimTypes.GivenName)?.Value;
                }
            }
            return base.SaveChanges(acceptAllChangesOnSuccess);
        }

        public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
        {
            if (httpContextAccessor.HttpContext.User.FindFirst(ClaimTypes.Role)?.Value != "admin")
            {
                foreach (var entityEntry in ChangeTracker.Entries().Where(e => e.State == EntityState.Added))
                {
                    if (entityEntry.Entity is IDataKey dataKey)
                        dataKey.DataKey = httpContextAccessor.HttpContext.User.FindFirst(ClaimTypes.GivenName)?.Value;
                }
            }
            return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        }


        private static void AddHierarchicalQueryFilter<T>(EntityTypeBuilder<T> builder, ComplaintSeriesDbContext _ctx)
         where T : class, IDataKey
        {
            builder.HasQueryFilter(x =>
            _ctx.httpContextAccessor.HttpContext.User.FindFirst(ClaimTypes.Role).Value == "admin" ?
                true : x.DataKey.Equals(_ctx.httpContextAccessor.HttpContext.User.FindFirst(ClaimTypes.GivenName).Value));
        }
    }
}