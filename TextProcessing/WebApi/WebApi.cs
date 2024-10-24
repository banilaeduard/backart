using System.Fabric;
using System.Net;
using System.Security.Cryptography.X509Certificates;
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
using YahooFeeder;
using AzureServices;
using Services.Storage;
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
using SqlTableRepository.CommitedOrders;
using AutoMapper;
using WebApi.Models;
using RepositoryContract;

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
                        //builder.Services.AddSwaggerGen();
                        var app = builder.Build();

                        //if (app.Environment.IsDevelopment())
                        //{
                        //app.UseSwagger();
                        //app.UseSwaggerUI();
                        //}

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
            services.AddSingleton(new MailSettings()
            {
                DaysBefore = int.Parse(Environment.GetEnvironmentVariable("days_before")!),
                Password = Environment.GetEnvironmentVariable("Password")!,
                User = Environment.GetEnvironmentVariable("User")!
            });

            services.AddSingleton(new ConnectionSettings()
            {
                ConnectionString = Environment.GetEnvironmentVariable("ConnectionString"),
                ExternalConnectionString = Environment.GetEnvironmentVariable("external_sql_server"),
                SqlQueryCache = Environment.GetEnvironmentVariable("path_to_sql"),
            });

            services.AddScoped<SaSToken, SaSToken>();
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<AzureFileStorage, AzureFileStorage>();
            services.AddScoped<EmailSender, EmailSender>();
            services.AddScoped<IStorageService, BlobAccessStorageService>();
            services.AddScoped<ITaskRepository, TaskRepository>();
            services.AddSingleton<Initializer>();
            services.AddScoped<IImportsRepository, OrdersImportsRepository>();
            services.AddScoped<ITicketEntryRepository, TicketEntryRepository>();
            services.AddScoped<IDataKeyLocationRepository, DataKeyLocationRepository>();
            services.AddScoped<IProductCodeRepository, ProductCodesRepository>();

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
            });

            MapperConfiguration config = new MapperConfiguration(cfg =>
            {
                cfg.CreateMap<AttachmentEntry, AttachmentModel>();
                cfg.CreateMap<AttachmentModel, AttachmentEntry>();
            });
            IMapper mapper = config.CreateMapper();
            services.AddSingleton(mapper);
        }

        /// <summary>
        /// Finds the ASP .NET Core HTTPS development certificate in development environment. Update this method to use the appropriate certificate for production environment.
        /// </summary>
        /// <returns>Returns the ASP .NET Core HTTPS development certificate</returns>
        private static X509Certificate2? GetCertificateFromStore()
        {
            string aspNetCoreEnvironment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")!;
            if (string.Equals(aspNetCoreEnvironment, "Development", StringComparison.OrdinalIgnoreCase))
            {
                const string aspNetHttpsOid = "1.3.6.1.4.1.311.84.1.1";
                const string CNName = "CN=localhost";
                using (X509Store store = new X509Store(StoreName.My, StoreLocation.LocalMachine))
                {
                    store.Open(OpenFlags.ReadOnly);
                    var certCollection = store.Certificates;
                    var currentCerts = certCollection.Find(X509FindType.FindByExtension, aspNetHttpsOid, true);
                    currentCerts = currentCerts.Find(X509FindType.FindByIssuerDistinguishedName, CNName, true);
                    return currentCerts.Count == 0 ? null : currentCerts[0];
                }
            }
            else
            {
                return GetCertificateFromStore2();
            }
        }

        private static X509Certificate2 GetCertificateFromStore2()
        {
            var store = new X509Store(StoreName.My, StoreLocation.LocalMachine);
            try
            {
                store.Open(OpenFlags.ReadOnly);
                var certCollection = store.Certificates;
                var currentCerts = certCollection.Find(X509FindType.FindBySubjectDistinguishedName, "CN=bartazeu.eastus.cloudapp.azure.com", false);
                return currentCerts.Count == 0 ? null : currentCerts[0];
            }
            finally
            {
                store.Close();
            }
        }
    }
}
