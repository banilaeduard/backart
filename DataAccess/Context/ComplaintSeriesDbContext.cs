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
        public DbSet<DataKeyLocation> DataKeyLocation { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Attachment>()
                .HasOne(p => p.Ticket)
                .WithMany(t => t.Attachments)
                .HasForeignKey(f => f.TicketId)
                .OnDelete(DeleteBehavior.ClientCascade);

            modelBuilder.Entity<CodeAttribute>()
                .ToTable("CodeAttributeSnapshot")
                .HasKey(t => new { t.Tag, t.InnerValue })
                .HasName("Id");

            modelBuilder.Entity<CodeLink>()
                .ToTable("CodeLinkSnapshot")
                .Ignore(t => t.Ancestors).Ignore(t => t.Children);

            modelBuilder.Entity<Ticket>()
                    .HasOne(t => t.Complaint)
                    .WithMany(t => t.Tickets)
                    .HasForeignKey(f => f.ComplaintId);
                    //.OnDelete(DeleteBehavior.ClientCascade);

            base.OnModelCreating(modelBuilder);
        }

        protected override void BeforeSave(EntityEntry entityEntry, string correlationId)
        {
            if (entityEntry.Entity is Ticket ticket)
            {
                entityEntry.Navigation("Attachments").Load();
                ticket.HasAttachments = ticket.Attachments.Count() > 0;

                var codeLinks = ticket.CodeLinks?.ToList();
                entityEntry.Navigation("CodeLinks").Load();
                ticket.CodeLinks = codeLinks;

                if (entityEntry.State == EntityState.Modified)
                {
                    entityEntry.Property("CodeValue").IsModified = false;
                }
            }
        }
    }
}