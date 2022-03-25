using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SolrNet;
using System.Collections.Generic;

namespace SolrIndexing
{
    public static class CollectionExtension
    {
        public static IServiceCollection configureSolr(this IServiceCollection services, IConfiguration Configuration)
        {
            var url = string.Format("{0}/{1}", Configuration["ConnectionStrings:SolrConnection"], "solr/documents");
            services.AddSolrNet<Dictionary<string, object>>(url);
            services.AddScoped<SolrCRUD>();
            return services;
        }
    }
}
