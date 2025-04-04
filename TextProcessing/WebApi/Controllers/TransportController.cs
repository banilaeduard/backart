using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RepositoryContract.ExternalReferenceGroup;
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
           ITransportRepository transportRepository,
           IExternalReferenceGroupRepository externalReferenceGroupRepository
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
            return Ok(mapper.Map<TransportModel>(await _transportRepository.GetTransport(transportId)));
        }

        [HttpPost]
        public async Task<IActionResult> SaveTransport(TransportModel transport)
        {
            return Ok(mapper.Map<TransportModel>(await _transportRepository.SaveTransport(mapper.Map<TransportEntry>(transport))));
        }

        [HttpPatch]
        public async Task<IActionResult> UpdateTransport(TransportModel transport, [FromQuery] int[] transportItemsToRemove)
        {
            return Ok(mapper.Map<TransportModel>(await _transportRepository.UpdateTransport(mapper.Map<TransportEntry>(transport), transportItemsToRemove ?? [])));
        }

        [HttpPost("attachments/{transportId}")]
        public async Task<IActionResult> SaveTransportAttachments(List<UserUpload> userUploads, int transportId, [FromQuery] int[] transportAttachmentsToRemove)
        {
            var externalReferenceGroupEntries = await _transportRepository.HandleExternalAttachmentRefs([.. userUploads.Select(mapper.Map<ExternalReferenceGroupEntry>)], transportId, transportAttachmentsToRemove);
            return Ok(externalReferenceGroupEntries.Select(mapper.Map<UserUpload>));
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
