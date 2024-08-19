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
        private DbSet<DataKeyLocation> locationKeys { get; set; }

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
                .ToTable("CodeLinkSnapshot");

            modelBuilder.Entity<Ticket>()
                    .HasOne(t => t.Complaint)
                    .WithMany(t => t.Tickets)
                    .HasForeignKey(f => f.ComplaintId)
                    .OnDelete(DeleteBehavior.ClientCascade);

            modelBuilder.Entity<ComplaintSeries>()
                .Navigation<Ticket>(t => t.Tickets).AutoInclude();

            modelBuilder.Entity<Ticket>()
                .Navigation<Attachment>(t => t.Attachments).AutoInclude();

            modelBuilder.Entity<Ticket>()
                .Navigation<CodeLink>(t => t.CodeLinks).AutoInclude();

            base.OnModelCreating(modelBuilder);
        }

        protected override void BeforeSave(EntityEntry entityEntry)
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
            else if (entityEntry.Entity is ComplaintSeries series)
            {
                var existingLocation = locationKeys.Where(t => t.name == series.DataKey.name).FirstOrDefault();
                if (existingLocation != null)
                {
                    this.Entry(series.DataKey).State = EntityState.Detached;
                    series.DataKeyId = existingLocation.Id;
                }
            }
        }
    }
}