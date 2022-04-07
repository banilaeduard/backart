using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SolrIndexing;
using System.Threading.Tasks;

namespace WebApi.Controllers
{
    [Authorize(Roles = "partener, admin")]
    public class SolrController : WebApiController2
    {
        SolrCRUD solrIndex;
        public SolrController(
            ILogger<SolrController> logger,
            SolrCRUD solrIndex) : base(logger)
        {
            this.solrIndex = solrIndex;
        }

        [HttpGet("{skip}/{take}/{query}")]
        public async Task<IActionResult> Find(int skip, int take, string query)
        {
            var solrResults = await solrIndex.query((skip-1) * take, take, query);
            return Ok(new {
                count = solrResults.NumFound,
                results = solrResults
            });

        }
    }
}
