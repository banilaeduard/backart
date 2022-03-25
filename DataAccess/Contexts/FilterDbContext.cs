using DataAccess.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace DataAccess.Context
{
    public class FilterDbContext : BaseContext
    {
        public DbSet<Filter> Filters { get; set; }
        public FilterDbContext(DbContextOptions<FilterDbContext> ctxBuilder,
            IBaseContextAccesor contextAccesor) : base(ctxBuilder, contextAccesor)
        {
        }

        protected override void BeforeSave(EntityEntry entityEntry)
        {
            // no op
        }
    }
}
