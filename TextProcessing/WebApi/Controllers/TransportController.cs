using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RepositoryContract.CommitedOrders;
using RepositoryContract.Transports;
using WebApi.Models;

namespace WebApi.Controllers
{
    [Authorize(Roles = "admin")]
    public class TransportController : WebApiController2
    {
        private readonly ITransportRepository transportRepository;
        public TransportController(
           ILogger<UsersController> logger,
           IMapper mapper,
           ICommitedOrdersRepository commitedOrdersRepository,
        ITransportRepository transportRepository
           ) : base(logger, mapper)
        {
            this.transportRepository = transportRepository;
        }

        [HttpGet]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> GetAll()
        {
            return Ok(await transportRepository.GetTransports());
        }

        [HttpGet("{transportId}")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> Get(int transportId)
        {
            return Ok(await transportRepository.GetTransport(transportId));
        }

        [HttpPost]
        public async Task<IActionResult> SaveTransport(TransportModel transport)
        {
            return Ok(await transportRepository.SaveTransport(mapper.Map<TransportEntry>(transport)));
        }

        [HttpPatch]
        public async Task<IActionResult> UpdateTransport(TransportModel transport)
        {
            return Ok(await transportRepository.UpdateTransport(mapper.Map<TransportEntry>(transport)));
        }

        [HttpDelete("{transportId}")]
        public async Task<IActionResult> DeleteTransport(int transportId)
        {
            await transportRepository.DeleteTransport(transportId);
            return Ok(new { success = true });
        }
    }
}
