namespace WebApi.Controllers
{
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.Extensions.Logging;
    using System.Linq;
    using global::WebApi.Models;
    using RepositoryContract.ProductCodes;
    using AzureSerRepositoryContract.ProductCodesvices;
    using AutoMapper;

    public class CodesController : WebApiController2
    {
        private IProductCodeRepository productCodeRepository;
        public CodesController(
            IProductCodeRepository productCodeRepository,
            IMapper mapper,
            ILogger<CodesController> logger) : base(logger, mapper)
        {
            this.productCodeRepository = productCodeRepository;
        }

        [HttpGet]
        [Authorize(Roles = "partener, admin")]
        public async Task<IActionResult> GetCodes()
        {
            CodeLinkModel map(ProductCodeEntry t)
            {
                return new CodeLinkModel()
                {
                    Id = t.PartitionKey + "_" + t.RowKey,
                    CodeDisplay = t.Name,
                    CodeValue = t.Code,
                    Level = t.Level,
                    CodeBar = t.Bar,
                    ParentCode = t.ParentCode,
                    RootCode = t.RootCode,
                    PartitionKey = t.PartitionKey,
                    RowKey = t.RowKey,
                    //barcode = BarCodeGenerator.GenerateDataUrlBarCode(t.CodeValue)
                };
            }
            var result = await productCodeRepository.GetProductCodes();

            return Ok(result.Select(map));
        }

        [HttpPost("delete/{partitionKey}/{rowKey}")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> DeleteCodes(string partitionKey, string rowKey)
        {
            CodeLinkModel map(ProductCodeEntry t)
            {
                return new CodeLinkModel()
                {
                    Id = t.PartitionKey + "_" + t.RowKey,
                    CodeDisplay = t.Name,
                    CodeValue = t.Code,
                    Level = t.Level,
                    CodeBar = t.Bar,
                    ParentCode = t.ParentCode,
                    RootCode = t.RootCode,
                    PartitionKey = t.PartitionKey,
                    RowKey = t.RowKey,
                    //barcode = BarCodeGenerator.GenerateDataUrlBarCode(t.CodeValue)
                };
            }
            await productCodeRepository.Delete<ProductCodeEntry>(partitionKey, rowKey);
            var result = await productCodeRepository.GetProductCodes();
            return Ok(result.Select(map));
        }

        [HttpPost("productstats")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> CreateProductStats(ProductStatsModel[] productStatsModels)
        {
            return Ok(await productCodeRepository.CreateProductStats([.. productStatsModels.Select(mapper.Map<ProductStatsEntry>)]));
        }

        [HttpGet("productstats")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> GetProductStats()
        {
            var stats= await productCodeRepository.GetProductStats();
            return Ok(stats ?? []);
        }
    }
}