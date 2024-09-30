using AutoMapper;
using DataAccess.Context;
using EntityDto;
using Microsoft.EntityFrameworkCore;
using RepositoryContract.Imports;
using Services.Storage;
using System.Text;

namespace SqlTableRepository.Orders
{
    public class OrdersImportsRepository : IImportsRepository
    {
        private ImportsDbContext importsDbContext;
        private IStorageService storageService;
        private IMapper mapper;

        private static MapperConfiguration config = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<ComandaVanzare, DataAccess.Entities.ComandaVanzareEntry>();
            cfg.CreateMap<DataAccess.Entities.ComandaVanzareEntry, ComandaVanzare>();
        });

        public OrdersImportsRepository(ImportsDbContext importsDbContext, IStorageService storageService)
        {
            this.importsDbContext = importsDbContext;
            this.storageService = storageService;
            mapper = config.CreateMapper();
        }

        public async Task<IList<ComandaVanzare>> GetImportOrders()
        {
            var ro = new DateTime(2014, 6, 1);
            var blob = storageService.Access("QImport/orders.txt", out var contentType);
            var sql = Encoding.UTF8.GetString(blob);

            var items = importsDbContext.ComandaVanzare.FromSqlRaw(sql).Where(t => t.DataDoc > ro).ToList();
            return items.Select(mapper.Map<ComandaVanzare>).ToList();
        }
    }
}
