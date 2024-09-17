namespace DataAccess.Context
{
    using DataAccess.Entities;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.ChangeTracking;

    public class CodeDbContext : BaseContext
    {
        public CodeDbContext(DbContextOptions<CodeDbContext> ctxBuilder, IBaseContextAccesor contextAccesor) :
            base(ctxBuilder, contextAccesor)
        {
        }
        public DbSet<CodeLink> Codes { get; set; }
        public DbSet<CodeAttribute> CodeAttribute { get; set; }
        public DbSet<CodeLinkNode> CodeLinkNode { get; set; }

        protected override void BeforeSave(EntityEntry entityEntry, string correlationId)
        {
            // no op
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<CodeAttribute>()
                .HasKey(t => new { t.Tag, t.InnerValue })
                .HasName("Id");

            modelBuilder.Entity<CodeLinkNode>().HasKey(p => new { p.ParentNode, p.ChildNode });

            modelBuilder.Entity<CodeLink>().HasMany(p => p.Ancestors)
                                        .WithOne(t => t.Child)
                                        .HasForeignKey(t => t.ChildNode)
                                        .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<CodeLink>().HasMany(p => p.Children)
                                        .WithOne(t => t.Parent)
                                        .HasForeignKey(t => t.ParentNode)
                                        .OnDelete(DeleteBehavior.Restrict);

            base.OnModelCreating(modelBuilder);
        }
    }
}