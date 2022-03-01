using Microsoft.Extensions.DependencyInjection;

namespace Storage
{
    public static class CollectionExtension
    {
            public static IServiceCollection configureStorage(this IServiceCollection services)
            {
                services.AddScoped<IStorageService, ImageStorageService>();
                return services;
            }
    }
}
