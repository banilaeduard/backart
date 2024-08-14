using DataAccess.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;


namespace DataAccess
{
    public static class CollectionExtension
    {
        private static void configureConnectionString(string defaultConnection, DbContextOptionsBuilder options)
        {
            options.UseMySql(
                defaultConnection,
                ServerVersion.AutoDetect(defaultConnection),
                b => b.MigrationsAssembly("DataAccess").UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery));
            return;
        }

        public static IServiceCollection configureDataAccess(this IServiceCollection services, string defaultConnection)
        {

            services.AddDbContext<ComplaintSeriesDbContext>(options => configureConnectionString(defaultConnection, options));
            services.AddDbContext<CodeDbContext>(options => configureConnectionString(defaultConnection, options));
            services.AddDbContext<FilterDbContext>(options => configureConnectionString(defaultConnection, options));
            services.AddDbContext<AppIdentityDbContext>(options => configureConnectionString(defaultConnection, options));

            services.AddScoped<NoFilterBaseContext>();
            services.AddSingleton<Initializer>();
            return services;
        }
    }
}
