using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RepositoryContract.Transport;

namespace WebApi.Controllers
{
    [Authorize(Roles = "admin")]
    public class TransportController : WebApiController2
    {
        private readonly ITransportRepository transportRepository;
        public TransportController(
           ILogger<UsersController> logger,
           IMapper mapper,
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
        public async Task<IActionResult> SaveTask(TransportEntry transport)
        {
            return Ok(await transportRepository.SaveTransport(transport));
        }

        [HttpPatch]
        public async Task<IActionResult> UpdateTask(TransportEntry transport)
        {
            return Ok(await transportRepository.UpdateTransport(transport));
        }
    }
}
