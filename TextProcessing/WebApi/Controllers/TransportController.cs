using System.Globalization;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RepositoryContract.ExternalReferenceGroup;
using RepositoryContract.Transports;
using V2.Interfaces;
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

        [HttpGet("{date}/{pageSize}")]
        public async Task<IActionResult> GetAllSince(string date, int pageSize)
        {
            var from = DateTime.Parse(date, CultureInfo.InvariantCulture);
            return Ok(await _transportRepository.GetTransports(from, pageSize));
        }

        [HttpGet("{transportId}")]
        public async Task<IActionResult> Get(int transportId)
        {
            return Ok(mapper.Map<TransportModel>(await _transportRepository.GetTransport(transportId)));
        }

        [HttpPost]
        public async Task<IActionResult> SaveTransport(TransportModel transport)
        {
            var saved = mapper.Map<TransportModel>(await _transportRepository.SaveTransport(mapper.Map<TransportEntry>(transport)));
            await GetService<IWorkLoadService>().ThrottlePublish(null);
            return Ok(saved);
        }

        [HttpPatch]
        public async Task<IActionResult> UpdateTransport(TransportModel transport, [FromQuery] int[] transportItemsToRemove)
        {
            var initial = await _transportRepository.GetTransport(transport.Id!.Value);
            var result = mapper.Map<TransportModel>(await _transportRepository.UpdateTransport(mapper.Map<TransportEntry>(transport), transportItemsToRemove ?? []));
            if (initial.CurrentStatus == "Pending" || result.CurrentStatus == "Pending"
                && (initial.CurrentStatus != result.CurrentStatus || initial.Delivered?.ToShortDateString() != result.Delivered?.ToShortDateString()))
            {
                await GetService<IWorkLoadService>().ThrottlePublish(null);
            }
            return Ok(result);
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
            await GetService<IWorkLoadService>().ThrottlePublish(null);
            return Ok(new { success = true });
        }
    }
}
