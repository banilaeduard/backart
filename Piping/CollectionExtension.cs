using Microsoft.Extensions.DependencyInjection;

namespace Piping
{
    public static class CollectionExtension
    {
        public static IServiceCollection configurePiping(this IServiceCollection services)
        {
            services.AddScoped<EnrichService>();
            return services;
        }
    }
}
