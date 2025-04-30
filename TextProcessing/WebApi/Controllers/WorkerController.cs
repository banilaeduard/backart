using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApi.Models;
using WebApi.Services;

namespace WebApi.Controllers
{
    public class WorkerController : WebApiController2
    {
        private readonly StructuraReport _structuraReport;
        public WorkerController(StructuraReport structuraReport,
            ILogger<WorkerController> logger, IMapper mapper) : base(logger, mapper)
        {
            _structuraReport = structuraReport;
        }

        [HttpGet("{workerName}")]
        [AllowAnonymous]
        public async Task<IEnumerable<ReportModel>> GetWorkLoad(string workerName)
        {
            var workItem = new WorkItem()
            {
                CodProdus = "MPALSUM35H21",
                Cantitate = 5,
                CodLocatie = "test",
                DeliveryDate = DateTime.Now.ToUniversalTime(),
                NumarComanda = "test_numar_comanda",
                NumeProdus = "Allegro Sif"
            };
            var model = new WorkerPriorityList([workItem, workItem, workItem, workItem]);
            var items = await _structuraReport.GenerateReport(workerName, model, model);

            return items;
        }
    }
}
