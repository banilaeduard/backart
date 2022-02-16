namespace WebApi.Entities
{
    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.Metadata.Builders;
    using Microsoft.AspNetCore.Http;

    using System.Linq;
    using System.Security.Claims;
    using System.Threading.Tasks;
    using System.Threading;
    using System.IO;

    public class ComplaintSeriesDbContext : DbContext
    {
        private IHttpContextAccessor httpContextAccessor;
        public ComplaintSeriesDbContext(DbContextOptions<ComplaintSeriesDbContext> ctxBuilder,
            IHttpContextAccessor httpContextAccessor) : base(ctxBuilder)
        {
            this.httpContextAccessor = httpContextAccessor;
        }
        public DbSet<ComplaintSeries> Complaints { get; set; }
        public DbSet<Ticket> Ticket { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Image>()
                .Ignore(t => t.Extension)
                .HasOne(p => p.Ticket)
                .WithMany("Images")
                .HasForeignKey(f => f.TicketId)
                .OnDelete(DeleteBehavior.ClientCascade);

            modelBuilder.Entity<CodeAttribute>()
                .ToTable("CodeAttributeSnapshot")
                .HasKey(t => new { t.Tag, t.InnerValue })
                .HasName("Id");

            modelBuilder.Entity<CodeLink>()
                .ToTable("CodeLinkSnapshot");

            AddHierarchicalQueryFilter(modelBuilder.Entity<ComplaintSeries>(), this);
            base.OnModelCreating(modelBuilder);
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
        }

        public override int SaveChanges(bool acceptAllChangesOnSuccess)
        {
            this.addDataContext(ChangeTracker);
            return base.SaveChanges(acceptAllChangesOnSuccess);
        }

        public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
        {
            this.addDataContext(ChangeTracker);
            return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        }

        private void addDataContext(Microsoft.EntityFrameworkCore.ChangeTracking.ChangeTracker ChangeTracker)
        {
            bool isAdmin = httpContextAccessor.HttpContext.User.FindFirst(ClaimTypes.Role)?.Value == "admin";

            foreach (var entityEntry in ChangeTracker.Entries().ToList())
            {
                if (entityEntry.State == EntityState.Added || entityEntry.State == EntityState.Modified)
                {
                    // we ensure the separation of data based on clients
                    if (entityEntry.State == EntityState.Added && entityEntry.Entity is IDataKey dataKey && !isAdmin)
                        dataKey.DataKey = httpContextAccessor.HttpContext.User.FindFirst(ClaimTypes.GivenName)?.Value;
                    else if (entityEntry.Entity is IDataKey)
                        entityEntry.Property("DataKey").IsModified = false;
                }
                if (entityEntry.Entity is Ticket ticket)
                {
                    entityEntry.Navigation("Images").Load();
                    ticket.HasImages = ticket.Images.Count() > 0;

                    var codeLinks = ticket.codeLinks?.ToList();
                    entityEntry.Navigation("codeLinks").Load();
                    ticket.codeLinks = codeLinks;
                }
            }
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