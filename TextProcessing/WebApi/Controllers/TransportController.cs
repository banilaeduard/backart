using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RepositoryContract.Transports;
using WebApi.Models;

namespace WebApi.Controllers
{
    [Authorize(Roles = "admin, basic")]
    public class TransportController : WebApiController2
    {
        private readonly ITransportRepository _transportRepository;

        public TransportController(
           ILogger<UsersController> logger,
           IMapper mapper,
           ITransportRepository transportRepository
           ) : base(logger, mapper)
        {
            _transportRepository = transportRepository;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            return Ok(await _transportRepository.GetTransports());
        }

        [HttpGet("{transportId}")]
        public async Task<IActionResult> Get(int transportId)
        {
            return Ok(await _transportRepository.GetTransport(transportId));
        }

        [HttpPost]
        public async Task<IActionResult> SaveTransport(TransportModel transport)
        {
            return Ok(await _transportRepository.SaveTransport(mapper.Map<TransportEntry>(transport)));
        }

        [HttpPatch]
        public async Task<IActionResult> UpdateTransport(TransportModel transport, [FromQuery] int[] transportItemsToRemove)
        {
            return Ok(await _transportRepository.UpdateTransport(mapper.Map<TransportEntry>(transport), transportItemsToRemove ?? []));
        }

        [HttpDelete("{transportId}")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> DeleteTransport(int transportId)
        {
            await _transportRepository.DeleteTransport(transportId);
            return Ok(new { success = true });
        }
    }
}
