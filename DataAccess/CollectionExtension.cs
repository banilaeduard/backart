using DataAccess.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;


namespace DataAccess
{
    public static class CollectionExtension
    {
        internal static void configureConnectionString(string defaultConnection, DbContextOptionsBuilder options)
        {
            if (!defaultConnection.Contains("datdat.database"))
            {
                options.UseMySql(
                    defaultConnection,
                    ServerVersion.AutoDetect(defaultConnection),
                    b => { });
            }
            else
            {
                options.UseSqlServer(defaultConnection, s =>
                { });
            }
            return;
        }

        public static IServiceCollection configureDataAccess(this IServiceCollection services, string defaultConnection)
        {

            services.AddDbContext<ComplaintSeriesDbContext>(options => configureConnectionString(defaultConnection, options));
            services.AddDbContext<CodeDbContext>(options => configureConnectionString(defaultConnection, options));
            services.AddDbContext<AppIdentityDbContext>(options => configureConnectionString(defaultConnection, options));
            services.AddDbContext<JobStatusContext>(options => configureConnectionString(defaultConnection, options));
            services.AddDbContext<ImportsDbContext>(options => configureConnectionString(defaultConnection, options));

            services.AddScoped<NoFilterBaseContext>();
            services.AddSingleton<Initializer>();
            return services;
        }
    }
}
