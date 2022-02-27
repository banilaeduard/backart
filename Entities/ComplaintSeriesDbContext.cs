namespace WebApi.Entities
{

    using Microsoft.EntityFrameworkCore.ChangeTracking;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.AspNetCore.Http;

    using System.Linq;

    public class ComplaintSeriesDbContext : BaseContext
    {
        public ComplaintSeriesDbContext(DbContextOptions<ComplaintSeriesDbContext> ctxBuilder,
            IHttpContextAccessor httpContextAccessor) : base(ctxBuilder, httpContextAccessor)
        {
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

            base.OnModelCreating(modelBuilder);
        }

        protected override void BeforeSave(EntityEntry entityEntry)
        {
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
}