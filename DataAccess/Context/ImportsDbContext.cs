using DataAccess.Entities;
using Microsoft.EntityFrameworkCore;

namespace DataAccess.Context
{
    public class ImportsDbContext : DbContext
    {
        private string comandaVanzareTable;
        public ImportsDbContext(DbContextOptions ctxBuilder) : base(ctxBuilder) { }

        public DbSet<ComandaVanzareEntry> ComandaVanzare { get; set; }
        public DbSet<DispozitieLivrareEntry> DispozitieLivrare { get; set; }
    }
}
