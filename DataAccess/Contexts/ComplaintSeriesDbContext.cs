namespace DataAccess.Context
{
    using Microsoft.EntityFrameworkCore.ChangeTracking;
    using Microsoft.EntityFrameworkCore;

    using System.Linq;
    using DataAccess.Entities;

    public class ComplaintSeriesDbContext : BaseContext
    {
        public ComplaintSeriesDbContext(DbContextOptions<ComplaintSeriesDbContext> ctxBuilder,
            IBaseContextAccesor contextAccesor) : base(ctxBuilder, contextAccesor)
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

            modelBuilder.Entity<ComplaintSeries>()
                .Navigation<Ticket>(t => t.Tickets).AutoInclude();

            modelBuilder.Entity<Ticket>()
                .Navigation<Image>(t => t.Images).AutoInclude();

            modelBuilder.Entity<Ticket>()
                .Navigation<CodeLink>(t => t.codeLinks).AutoInclude();

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

                if (entityEntry.State == EntityState.Modified)
                {
                    entityEntry.Property("CodeValue").IsModified = false;
                }
            }
        }
    }
}