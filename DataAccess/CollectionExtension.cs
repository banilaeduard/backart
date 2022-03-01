using DataAccess.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DataAccess
{
    public static class CollectionExtension
    {        
        private static void configureConnectionString(IConfiguration Configuration, DbContextOptionsBuilder options)
        {
            var sqlOpt = Configuration["ConnectionStrings:instanceType"];
            switch (sqlOpt)
            {
                case "mysql":
                    options.UseMySql(Configuration["ConnectionStrings:DefaultConnection"], ServerVersion.AutoDetect(Configuration["ConnectionStrings:DefaultConnection"]));
                    return;
                default:
                    options.UseSqlServer(Configuration["ConnectionStrings:DefaultConnection"]);
                    return;
            }
        }
        public static IServiceCollection configureDataAccess(this IServiceCollection services, IConfiguration Configuration)
        {
            services.AddDbContext<ComplaintSeriesDbContext>(options => configureConnectionString(Configuration, options));
            services.AddDbContext<CodeDbContext>(options => configureConnectionString(Configuration, options));
            services.AddDbContext<AppIdentityDbContext>(options => configureConnectionString(Configuration, options));

            services.AddScoped<NoFilterBaseContext>();
            return services;
        }
    }
}
