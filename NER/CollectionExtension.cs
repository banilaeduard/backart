using Microsoft.Extensions.DependencyInjection;

namespace NER
{
    public static class CollectionExtension
    {
        public static IServiceCollection configureNER(this IServiceCollection services)
        {
            services.AddSingleton<WordTokenizer>();
            services.AddSingleton<SentenceTokenizer>();
            services.AddSingleton<NameFinder>();
            services.AddSingleton<HtmlStripper>();
            return services;
        }
    }
}
