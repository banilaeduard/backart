using AutoMapper;
using AzureFabricServices;
using AzureServices;
using AzureTableRepository.CommitedOrders;
using AzureTableRepository.DataKeyLocation;
using AzureTableRepository.Orders;
using AzureTableRepository.ProductCodes;
using AzureTableRepository.Report;
using AzureTableRepository.Tickets;
using Dapper;
using DataAccess;
using EntityDto.ExternalReferenceGroup;
using EntityDto.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using ProjectKeys;
using RepositoryContract;
using RepositoryContract.CommitedOrders;
using RepositoryContract.DataKeyLocation;
using RepositoryContract.ExternalReferenceGroup;
using RepositoryContract.Imports;
using RepositoryContract.Orders;
using RepositoryContract.ProductCodes;
using RepositoryContract.Report;
using RepositoryContract.Tasks;
using RepositoryContract.Tickets;
using RepositoryContract.Transports;
using RepositoryServices;
using ServiceImplementation;
using ServiceImplementation.Caching;
using ServiceInterface;
using ServiceInterface.Storage;
using SqlTableRepository.CommitedOrders;
using SqlTableRepository.ExternalReferenceGroup;
using SqlTableRepository.Orders;
using SqlTableRepository.ProductCodes;
using SqlTableRepository.Tasks;
using SqlTableRepository.Transport;
using System.Data;
using System.Fabric;
using WebApi.Models;
using WebApi.Services;
using WordDocumentServices;
using WordDocumentServices.Services;

namespace WebApi
{
    internal static class ServiceImplementation
    {
        internal static void ConfigureServices(IServiceCollection services, StatelessServiceContext serviceContext)
        {
            services.configureDataAccess(Environment.GetEnvironmentVariable(KeyCollection.ConnectionString));
            services.AddSingleton(new ConnectionSettings()
            {
                ConnectionString = Environment.GetEnvironmentVariable(KeyCollection.ConnectionString)!,
                ExternalConnectionString = Environment.GetEnvironmentVariable(KeyCollection.ExternalServer)!,
                SqlQueryCache = Environment.GetEnvironmentVariable(KeyCollection.PathToSql)!,
            });

            services.Configure<ApiBehaviorOptions>(options =>
            {
                options.SuppressModelStateInvalidFilter = true;
            });


            SqlMapper.AddTypeHandler(new DateTimeHandler());

            services.AddSingleton<Initializer>();
            services.AddSingleton<TableStorageService>();
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
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<AzureFileStorage, AzureFileStorage>();
            services.AddScoped<BlobAccessStorageService, BlobAccessStorageService>();
            services.AddScoped<EmailSender, EmailSender>();
            services.AddScoped<IStorageService, BlobAccessStorageService>();
            services.AddScoped<IWorkflowTrigger, QueueService>();
            services.AddScoped<IImportsRepository, OrdersImportsRepository<AzureFileStorage>>();
            services.AddScoped<ITicketEntryRepository, TicketEntryRepository>();
            services.AddScoped<IDataKeyLocationRepository, DataKeyLocationRepository>();
            services.AddScoped<IReportEntryRepository, ReportEntryRepository>();
            services.AddScoped<ITransportRepository, TransportRepository>();
            services.AddScoped<IExternalReferenceGroupRepository, ExternalReferenceGroupSql>();
            services.AddScoped<ITemplateDocumentWriter, TemplateDocWriter>((provider) => new TemplateDocWriter(Stream.Null, provider.GetRequiredService<ICryptoService>()));
            services.AddScoped<StructuraReport, StructuraReport>();
            services.AddScoped<StructuraReportWriter, StructuraReportWriter>();
            services.AddScoped<SimpleReport, SimpleReport>();
#if !TEST
            services.AddScoped<IProductCodeRepository, ProductCodesRepository>();
            services.AddScoped<IOrdersRepository, OrdersRepository>();
            services.AddScoped<ICommitedOrdersRepository, CommitedOrdersRepository>();
#else
            services.AddScoped<IOrdersRepository, OrdersRepositorySql>();
            services.AddScoped<ICommitedOrdersRepository, CommitedOrdersRepositorySql<AzureFileStorage>>();
            services.AddScoped<IProductCodeRepository, ProductCodesRepository>();
            //NOT READY YET FOR THIS
            //services.AddScoped<IProductCodeRepository, ProductCodesRepositorySql>();
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

                cfg.CreateMap<OrderModel, OrderEntry>();
                cfg.CreateMap<OrderEntry, OrderModel>();

                cfg.CreateMap<TransportEntry, TransportModel>()
                    .ForMember(x => x.UserUploads, opt => opt.MapFrom(t => t.ExternalReferenceEntries)).AfterMap((src, dest) =>
                    {
                        if (!dest.UserUploads?.Any() == true) dest.UserUploads = null;
                    });
                cfg.CreateMap<TransportModel, TransportEntry>()
                    .ForMember(x => x.ExternalReferenceEntries, opt => opt.MapFrom(t => t.UserUploads));

                cfg.CreateMap<TransportItemEntry, TransportItemModel>();
                cfg.CreateMap<TransportItemModel, TransportItemEntry>();

                cfg.CreateMap<ExternalReferenceGroup, ExternalReference>()
                    .ForMember(x => x.Id, opt => opt.MapFrom(t => t.G_Id));

                cfg.CreateMap<UserUpload, ExternalReferenceGroupEntry>()
                    .ForMember(x => x.ExternalGroupId, opt => opt.MapFrom(t => t.Path))
                    .ForMember(x => x.G_Id, opt => opt.MapFrom(t => t.Id))
                    .ForMember(x => x.Date, opt => opt.MapFrom(t => t.Created));
                cfg.CreateMap<ExternalReferenceGroupEntry, UserUpload>()
                    .ForMember(x => x.Path, opt => opt.MapFrom(t => t.ExternalGroupId))
                    .ForMember(x => x.Id, opt => opt.MapFrom(t => t.G_Id))
                    .ForMember(x => x.Created, opt => opt.MapFrom(t => t.Date));
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
