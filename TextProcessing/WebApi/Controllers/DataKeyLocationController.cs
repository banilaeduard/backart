using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RepositoryContract.DataKeyLocation;

namespace WebApi.Controllers
{
    [Authorize(Roles = "admin")]
    public class DataKeyLocationController : WebApiController2
    {
        IDataKeyLocationRepository dataKeyLocationRepository;
        public DataKeyLocationController(
            ILogger<DataKeyLocationController> logger,
            IDataKeyLocationRepository dataKeyLocationRepository
            ) : base(logger)
        {
            this.dataKeyLocationRepository = dataKeyLocationRepository;
        }

        [HttpGet]
        public async Task<IActionResult> GetLocations()
        {
            return Ok(await dataKeyLocationRepository.GetLocations());
        }

        [HttpPatch]
        public async Task<IActionResult> UpdateLocation([FromBody] DataKeyLocationEntry[] locations)
        {
            await dataKeyLocationRepository.UpdateLocation(locations);
            return Ok();
        }

        [HttpPost]
        public async Task<IActionResult> AddLocation([FromBody] DataKeyLocationEntry[] locations)
        {
            await dataKeyLocationRepository.InsertLocation(locations);
            return Ok();
        }

        [HttpDelete("{partitionKey}/{rowKey}")]
        public async Task<IActionResult> DeleteLocation(string partitionKey, string rowKey)
        {
            await dataKeyLocationRepository.DeleteLocation([new() { PartitionKey = partitionKey, RowKey = rowKey }]);
            return Ok();
        }
    }
}
