using AzureFabricServices;
using AzureServices;
using AzureTableRepository.DataKeyLocation;
using AzureTableRepository.Tickets;
using DataAccess;
using RepositoryContract.CommitedOrders;
using RepositoryContract.DataKeyLocation;
using RepositoryContract.Imports;
using RepositoryContract.Orders;
using RepositoryContract.ProductCodes;
using RepositoryContract.Report;
using RepositoryContract.Tasks;
using RepositoryContract.Tickets;
using RepositoryContract.Transports;
using ServiceImplementation.Caching;
using ServiceImplementation;
using ServiceInterface.Storage;
using ServiceInterface;
using SqlTableRepository.CommitedOrders;
using SqlTableRepository.Orders;
using SqlTableRepository.ProductCodes;
using SqlTableRepository.Tasks;
using SqlTableRepository.Transport;
using System.Fabric;
using WebApi.Services;
using AutoMapper;
using Dapper;
using System.Data;
using WebApi.Models;
using RepositoryContract;

namespace WebApi
{
    internal static class ServiceImplementation
    {
        internal static void ConfigureServices(IServiceCollection services, StatelessServiceContext serviceContext)
        {
            services.configureDataAccess(Environment.GetEnvironmentVariable("ConnectionString"));
            services.AddSingleton(new ConnectionSettings()
            {
                ConnectionString = Environment.GetEnvironmentVariable("ConnectionString")!,
                ExternalConnectionString = Environment.GetEnvironmentVariable("external_sql_server")!,
                SqlQueryCache = Environment.GetEnvironmentVariable("path_to_sql")!,
            });

            SqlMapper.AddTypeHandler(new DateTimeHandler());

            services.AddSingleton<Initializer>();
            services.AddSingleton<IMetadataService, FabricMetadataService>();
            services.AddSingleton<ITaskRepository, TaskRepository>();
            services.AddSingleton<ICryptoService, CryptoService>();
            services.AddSingleton<ICacheManager<CommitedOrderEntry>, LocalCacheManager<CommitedOrderEntry>>();
            services.AddSingleton<ICacheManager<DataKeyLocationEntry>, LocalCacheManager<DataKeyLocationEntry>>();
            services.AddSingleton<ICacheManager<OrderEntry>, LocalCacheManager<OrderEntry>>();
            services.AddSingleton<ICacheManager<ProductCodeEntry>, LocalCacheManager<ProductCodeEntry>>();
            services.AddSingleton<ICacheManager<ProductStatsEntry>, LocalCacheManager<ProductStatsEntry>>();
            services.AddSingleton<ICacheManager<ProductCodeStatsEntry>, LocalCacheManager<ProductCodeStatsEntry>>();
            services.AddSingleton<ICacheManager<TicketEntity>, LocalCacheManager<TicketEntity>>();
            services.AddSingleton<ICacheManager<AttachmentEntry>, LocalCacheManager<AttachmentEntry>>();

            services.AddScoped<SaSToken, SaSToken>();
            services.AddScoped<ReclamatiiReport, ReclamatiiReport>();
            services.AddScoped<StructuraReport, StructuraReport>();
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<AzureFileStorage, AzureFileStorage>();
            services.AddScoped<EmailSender, EmailSender>();
            services.AddScoped<IStorageService, BlobAccessStorageService>();
            services.AddScoped<IWorkflowTrigger, QueueService>();
            services.AddScoped<IImportsRepository, OrdersImportsRepository>();
            services.AddScoped<ITicketEntryRepository, TicketEntryRepository>();
            services.AddScoped<IDataKeyLocationRepository, DataKeyLocationRepository>();
            services.AddScoped<IReportEntryRepository, ReportEntryRepository>();
            services.AddScoped<ITransportRepository, TransportRepository>();
#if (RELEASE)
            services.AddScoped<IProductCodeRepository, ProductCodesRepository>();
            services.AddScoped<IOrdersRepository, OrdersRepository>();
            services.AddScoped<ICommitedOrdersRepository, CommitedOrdersRepository>();
#elif (DEBUG)
            services.AddScoped<IOrdersRepository, OrdersRepositorySql>();
            services.AddScoped<ICommitedOrdersRepository, CommitedOrdersRepositorySql>();
            services.AddScoped<IProductCodeRepository, ProductCodesRepositorySql>();
#endif

            // MAPPINGS FOR AUTOMAPPER
            MapperConfiguration config = new MapperConfiguration(cfg =>
            {
                cfg.CreateMap<AttachmentEntry, AttachmentModel>();
                cfg.CreateMap<AttachmentModel, AttachmentEntry>();

                cfg.CreateMap<ProductStatsEntry, ProductStatsModel>();
                cfg.CreateMap<ProductStatsModel, ProductStatsEntry>();

                cfg.CreateMap<ProductCodeEntry, ProductModel>();
                cfg.CreateMap<ProductModel, ProductCodeEntry>();

                cfg.CreateMap<ProductCodeStatsModel, ProductCodeStatsEntry>();
                cfg.CreateMap<ProductCodeStatsEntry, ProductCodeStatsModel>();

                cfg.CreateMap<OrderModel, RepositoryContract.Orders.OrderEntry>();
                cfg.CreateMap<RepositoryContract.Orders.OrderEntry, OrderModel>();

                cfg.CreateMap<TransportEntry, TransportModel>();
                cfg.CreateMap<TransportModel, TransportEntry>();

                cfg.CreateMap<TransportItemEntry, TransportItemModel>();
                cfg.CreateMap<TransportItemModel, TransportItemEntry>();
            });

            IMapper mapper = config.CreateMapper();
            services.AddSingleton(mapper);
        }


        private class DateTimeHandler : SqlMapper.TypeHandler<DateTime>
        {
            public override void SetValue(IDbDataParameter parameter, DateTime value)
            {
                parameter.Value = value;
            }

            public override DateTime Parse(object value)
            {
                var v = (DateTime)value;
                return v.Kind == DateTimeKind.Unspecified ? DateTime.SpecifyKind((DateTime)value, DateTimeKind.Utc) : v;
            }
        }
    }
}
