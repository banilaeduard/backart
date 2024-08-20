using DataAccess.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace DataAccess.Context
{
    public class JobStatusContext : BaseContext
    {
        public JobStatusContext(DbContextOptions ctxBuilder,
    IBaseContextAccesor baseContextAccesor) : base(ctxBuilder, baseContextAccesor)
        {
        }

        public DbSet<JobStatusLog> JobStatus { get; set; }
        public DbSet<MailSourceConfig> MailSourceConfigs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<JobStatusLog>()
                .ToTable("jobstatus")
                .HasKey(t => t.Id);

            base.OnModelCreating(modelBuilder);
        }

        protected override void BeforeSave(EntityEntry entityEntry, string correlationId)
        {
            if (entityEntry.State == EntityState.Added && entityEntry.Entity is JobStatusLog jobStatus)
            {
                {
                    jobStatus.CorelationId = correlationId;
                }
            }
        }
    }
}
