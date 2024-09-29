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
        public async Task<IActionResult> UpdateLocation([FromBody] DataKeyLocationEntry location)
        {
            await dataKeyLocationRepository.UpdateLocation(location);
            return Ok(location);
        }

        [HttpPost]
        public async Task<IActionResult> AddLocation([FromBody] DataKeyLocationEntry location)
        {
            await dataKeyLocationRepository.InsertLocation(location);
            return Ok(location);
        }

        [HttpDelete("{partitionKey}/{rowKey}")]
        public async Task<IActionResult> DeleteLocation(string partitionKey, string rowKey)
        {
            await dataKeyLocationRepository.DeleteLocation(new() { PartitionKey = partitionKey, RowKey = rowKey });
            return Ok();
        }
    }
}
