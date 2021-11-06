namespace WebApi.Helpers
{
    using Microsoft.EntityFrameworkCore;
    using WebApi.Entities;

    public class DataContext : DbContext
    {
        public DbSet<User> Users { get; set; }

        public DataContext(DbContextOptions<DataContext> options) : base(options) { }
    }
}