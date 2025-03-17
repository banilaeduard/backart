using System.Fabric;
using System.Net;
using Microsoft.ServiceFabric.Services.Communication.AspNetCore;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Microsoft.ServiceFabric.Services.Runtime;
using DataAccess;
using System.Text.Json.Serialization;
using DataAccess.Context;
using DataAccess.Entities;
using Microsoft.AspNetCore.Identity;
using System.Text;
using WebApi.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Diagnostics;
using AzureServices;
using RepositoryContract.Orders;
using AzureTableRepository.Orders;
using RepositoryContract.CommitedOrders;
using AzureTableRepository.CommitedOrders;
using RepositoryContract.ProductCodes;
using AzureTableRepository.ProductCodes;
using RepositoryContract.DataKeyLocation;
using AzureTableRepository.DataKeyLocation;
using RepositoryContract.Tickets;
using AzureTableRepository.Tickets;
using RepositoryContract.Imports;
using SqlTableRepository.Orders;
using RepositoryContract.Tasks;
using SqlTableRepository.Tasks;
using AutoMapper;
using WebApi.Models;
using RepositoryContract;
using AzureSerRepositoryContract.ProductCodesvices;
using SqlTableRepository.CommitedOrders;
using RepositoryContract.Report;
using RepositoryContract.Transports;
using SqlTableRepository.Transport;
using Dapper;
using System.Data;
using ServiceInterface.Storage;
using ServiceImplementation;
using AzureFabricServices;
using AzureTableRepository;
using ServiceInterface;

namespace WebApi
{
    /// <summary>
    /// The FabricRuntime creates an instance of this class for each service type instance.
    /// </summary>
    internal sealed class WebApi : StatelessService
    {
        public WebApi(StatelessServiceContext context)
            : base(context)
        { }

        /// <summary>
        /// Optional override to create listeners (like tcp, http) for this service instance.
        /// </summary>
        /// <returns>The collection of listeners.</returns>
        protected override IEnumerable<ServiceInstanceListener> CreateServiceInstanceListeners()
        {
            var aspNetCoreEnvironment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")!;
            var endpoint = aspNetCoreEnvironment.Equals("Development") ? "ServiceEndpoint" : "EndpointHttps";
            return [
                new ServiceInstanceListener(serviceContext =>
                    new KestrelCommunicationListener(serviceContext, endpoint,
                    (url, listener) =>
                    {
                        //ServiceEventSource.Current.ServiceMessage(serviceContext, $"Starting Kestrel on {url}");

                        var builder = WebApplication.CreateBuilder();
                        builder.Services.AddSingleton(serviceContext);
                        builder.Services.AddControllers().AddJsonOptions(opt =>
                                                        {
                                                            opt.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
                                                            opt.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
                                                            opt.JsonSerializerOptions.NumberHandling = JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString;
                                                            opt.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
                                                            opt.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
                                                        });
                        ConfigureServices(builder.Services, serviceContext);

                        builder.WebHost
                                    .UseKestrel(opt =>
                                    {
                                        int port = serviceContext.CodePackageActivationContext.GetEndpoint(endpoint).Port;
                                        opt.Listen(IPAddress.IPv6Any, port, listenOptions =>
                                        {
                                            //listenOptions.UseHttps(GetCertificateFromStore()!);
                                        });
                                    })
                                    .UseContentRoot(Directory.GetCurrentDirectory())
                                    .UseServiceFabricIntegration(listener, ServiceFabricIntegrationOptions.None)
                                    .UseUrls(url);
                        builder.Services.AddEndpointsApiExplorer();
                        var app = builder.Build();

                         app.UseCors(x => x
                            .SetIsOriginAllowed(origin => true)
                            .AllowAnyMethod()
                            .AllowAnyHeader()
                            .AllowCredentials());

                        app.UseRouting();
                        app.UseAuthentication();
                        app.UseAuthorization();
                        app.MapControllers();
                        app.UseExceptionHandler(cfg => cfg.Run(async context => {
                            var exceptionHandlerPathFeature = context.Features.Get<IExceptionHandlerPathFeature>()!;
                            var exception = exceptionHandlerPathFeature.Error;
                            ServiceEventSource.Current.ServiceMessage(serviceContext, "Error. {0} . StackTrace: {1}", exception.Message, exception.StackTrace ?? "");
                        }));

                        app.Services.GetRequiredService<IServiceScopeFactory>()
                            .CreateScope().ServiceProvider
                            .GetRequiredService<Initializer>()
                            .ExecuteAsync(CancellationToken.None).GetAwaiter().GetResult();

                        return app;
                    }), "bart")
            ];
        }

        public void ConfigureServices(IServiceCollection services, StatelessServiceContext serviceContext)
        {
            services.Configure<CookiePolicyOptions>(options =>
            {
                options.CheckConsentNeeded = context => true;
                options.MinimumSameSitePolicy = SameSiteMode.None;
            });

            services.configureDataAccess(Environment.GetEnvironmentVariable("ConnectionString"));

            var cfg = Context.CodePackageActivationContext.GetConfigurationPackageObject("Config");

            services.AddSingleton(new ConnectionSettings()
            {
                ConnectionString = Environment.GetEnvironmentVariable("ConnectionString")!,
                ExternalConnectionString = Environment.GetEnvironmentVariable("external_sql_server")!,
                SqlQueryCache = Environment.GetEnvironmentVariable("path_to_sql")!,
            });
            services.AddSingleton<Initializer>();
            services.AddSingleton<IMetadataService, FabricMetadataService>();
            services.AddSingleton<ITaskRepository, TaskRepository>();
            services.AddSingleton<ICryptoService, CryptoService>();
            services.AddSingleton<ICacheManager<CommitedOrderEntry>, CacheManager<CommitedOrderEntry>>();
            services.AddSingleton<ICacheManager<DataKeyLocationEntry>, CacheManager<DataKeyLocationEntry>>();
            services.AddSingleton<ICacheManager<OrderEntry>, CacheManager<OrderEntry>>();
            services.AddSingleton<ICacheManager<ProductCodeEntry>, CacheManager<ProductCodeEntry>>();
            services.AddSingleton<ICacheManager<ProductStatsEntry>, CacheManager<ProductStatsEntry>>();
            services.AddSingleton<ICacheManager<ProductCodeStatsEntry>, CacheManager<ProductCodeStatsEntry>>();
            services.AddSingleton<ICacheManager<TicketEntity>, CacheManager<TicketEntity>>();
            services.AddSingleton<ICacheManager<AttachmentEntry>, CacheManager<AttachmentEntry>>();

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
            services.AddScoped<IProductCodeRepository, ProductCodesRepository>();
            services.AddScoped<IReportEntryRepository, ReportEntryRepository>();
            services.AddScoped<ITransportRepository, TransportRepository>();
#if (DEBUG)
            services.AddScoped<IOrdersRepository, OrdersRepository>();
            services.AddScoped<ICommitedOrdersRepository, CommitedOrdersRepository>();
#elif (RELEASE)
            services.AddScoped<IOrdersRepository, OrdersRepositorySql>();
            services.AddScoped<ICommitedOrdersRepository, CommitedOrdersRepositorySql>();
#endif
            services.AddIdentity<AppIdentityUser, AppIdentityRole>()
                    .AddEntityFrameworkStores<AppIdentityDbContext>()
                    .AddDefaultTokenProviders();

            var tokenKey = Environment.GetEnvironmentVariable("Secret")!;
            var key = Encoding.ASCII.GetBytes(tokenKey);
            SqlMapper.AddTypeHandler(new DateTimeHandler());

            services.AddAuthentication(x =>
            {
                x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(x =>
            {
                x.RequireHttpsMetadata = false;
                x.SaveToken = true;
                x.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ValidateLifetime = false,
                };
                x.IncludeErrorDetails = true;
            });

            services.Configure<IdentityOptions>(opts =>
            {
                opts.User.RequireUniqueEmail = true;
                opts.Password.RequiredLength = 8;
                opts.Password.RequireNonAlphanumeric = false;
                opts.Password.RequireUppercase = false;
                opts.SignIn.RequireConfirmedEmail = false;
                opts.Lockout.MaxFailedAccessAttempts = 30;
            });

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
    }

    public class DateTimeHandler : SqlMapper.TypeHandler<DateTime>
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
