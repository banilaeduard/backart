namespace WebApi.Controllers
{
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.Extensions.Logging;
    using System.Linq;
    using global::WebApi.Models;
    using RepositoryContract.ProductCodes;
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

            var result = await productCodeRepository.GetProductCodes();
            var stats = await productCodeRepository.GetProductCodeStatsEntry();
            return Ok(result.Select(Map(stats)));
        }

        [HttpPost("delete/{partitionKey}/{rowKey}")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> DeleteCodes(string partitionKey, string rowKey)
        {
            await productCodeRepository.Delete(new ProductCodeEntry() { PartitionKey = partitionKey, RowKey = rowKey });
            var result = await productCodeRepository.GetProductCodes();
            return Ok(result.Select(Map([])));
        }

        [HttpPatch]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> UpsertCodes(CodeLinkModel[] codes)
        {
            var result = codes.Select(MapReverse()).ToArray();
            await productCodeRepository.UpsertCodes(result);
            return Ok();
        }

        [HttpPost("productstats")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> CreateProductStats(ProductStatsModel[] productStatsModels)
        {
            return Ok(await productCodeRepository.CreateProductStats([.. productStatsModels.Select(mapper.Map<ProductStatsEntry>)]));
        }

        [HttpPost("productcodestats")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> CreateProductStats(ProductCodeStatsModel[] productCodeStatsModel)
        {
            return Ok(await productCodeRepository.CreateProductCodeStatsEntry([.. productCodeStatsModel.Select(mapper.Map<ProductCodeStatsEntry>)]));
        }

        [HttpGet("productstats")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> GetProductStats()
        {
            var stats = await productCodeRepository.GetProductStats();
            return Ok(stats ?? []);
        }

        Func<ProductCodeEntry, CodeLinkModel> Map(IList<ProductCodeStatsEntry> stats) => (ProductCodeEntry t) =>
            {
                var stat = stats.Where(x => x.PartitionKey == t.Code).Select(mapper.Map<ProductCodeStatsModel>).FirstOrDefault();
                return new CodeLinkModel()
                {
                    Id = t.Id > 0 ? t.Id.ToString() : (t.PartitionKey + "_" + t.RowKey),
                    CodeDisplay = t.Name,
                    CodeValue = t.Code,
                    Level = t.Level,
                    CodeBar = t.Bar,
                    ParentCode = t.ParentCode,
                    RootCode = t.RootCode,
                    PartitionKey = t.PartitionKey,
                    RowKey = t.RowKey,
                    ProductCodeStats = stat,
                    ProductCodeStats_Id = stat != null ? $"{stat.StatsPartitionKey}_{stat.StatsRowKey}" : ""
                    //barcode = BarCodeGenerator.GenerateDataUrlBarCode(t.CodeValue)
                };
            };

        Func<CodeLinkModel, ProductCodeEntry> MapReverse() => (CodeLinkModel t) =>
        {
            return new ProductCodeEntry()
            {
                Name = t.CodeDisplay,
                Code = t.CodeValue,
                Level = t.Level,
                Bar = t.CodeBar,
                ParentCode = t.ParentCode,
                RootCode = t.RootCode,
                PartitionKey = t.PartitionKey,
                RowKey = t.RowKey,
            };
        };
    }
}