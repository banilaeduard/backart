using Microsoft.Extensions.DependencyInjection;

namespace core
{
    public static class CollectionExtension
    {
            public static IServiceCollection configureCoreProject(this IServiceCollection services)
            {
                return services;
            }
    }
}
