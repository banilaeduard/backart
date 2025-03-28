﻿using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RepositoryContract.CommitedOrders;
using RepositoryContract.Transports;
using ServiceInterface.Storage;
using WebApi.Models;

namespace WebApi.Controllers
{
    [Authorize(Roles = "admin, basic")]
    public class TransportController : WebApiController2
    {
        private readonly ITransportRepository transportRepository;
        private readonly IWorkflowTrigger workflowTrigger;
        public TransportController(
           ILogger<UsersController> logger,
           IMapper mapper,
           IWorkflowTrigger workflowTrigger,
           ICommitedOrdersRepository commitedOrdersRepository,
        ITransportRepository transportRepository
           ) : base(logger, mapper)
        {
            this.transportRepository = transportRepository;
            this.workflowTrigger = workflowTrigger;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            return Ok(await transportRepository.GetTransports());
        }

        [HttpGet("{transportId}")]
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
        public async Task<IActionResult> UpdateTransport(TransportModel transport, [FromQuery] int[] transportItemsToRemove)
        {
            return Ok(await transportRepository.UpdateTransport(mapper.Map<TransportEntry>(transport), transportItemsToRemove));
        }

        [HttpDelete("{transportId}")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> DeleteTransport(int transportId)
        {
            await transportRepository.DeleteTransport(transportId);
            return Ok(new { success = true });
        }
    }
}
