using System.Fabric;
using System.Net;
using Microsoft.ServiceFabric.Services.Communication.AspNetCore;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Microsoft.ServiceFabric.Services.Runtime;
using System.Text.Json.Serialization;
using DataAccess.Context;
using DataAccess.Entities;
using Microsoft.AspNetCore.Identity;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Diagnostics;
using ProjectKeys;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;

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
                        var builder = WebApplication.CreateBuilder();
                        builder.Services.AddControllers().AddJsonOptions(opt =>
                                                        {
                                                            opt.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
                                                            opt.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
                                                            opt.JsonSerializerOptions.NumberHandling = JsonNumberHandling.AllowReadingFromString;
                                                            opt.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
                                                            opt.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
                                                            opt.JsonSerializerOptions.AllowTrailingCommas = true;
                                                        });
                        ConfigureServices(builder.Services, serviceContext);

                        builder.WebHost.UseKestrel()
                                    //.UseKestrel(opt =>
                                    //{
                                    //    int port = serviceContext.CodePackageActivationContext.GetEndpoint(endpoint).Port;
                                    //    opt.Listen(IPAddress.IPv6Any, port, listenOptions =>
                                    //    {
                                    //        //listenOptions.UseHttps(GetCertificateFromStore()!);
                                    //    });
                                    //})
                                    .UseContentRoot(Directory.GetCurrentDirectory())
                                    .UseServiceFabricIntegration(listener, ServiceFabricIntegrationOptions.None)
                                    .UseUrls(url);

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
                            var logger = context.RequestServices.GetRequiredService<ILogger<WebApi>>();
                            var exceptionHandlerPathFeature = context.Features.Get<IExceptionHandlerPathFeature>()!;
                            var exception = exceptionHandlerPathFeature.Error;
                            logger.LogError(new EventId(22), "Error. {0} . StackTrace: {1}", exception.Message, exception.StackTrace ?? "");
                        }));

                        //app.Services.GetRequiredService<IServiceScopeFactory>()
                        //    .CreateScope().ServiceProvider
                        //    .GetRequiredService<Initializer>()
                        //    .ExecuteAsync(CancellationToken.None).GetAwaiter().GetResult();

                        return app;
                    }), "bart")
            ];
        }

        public void ConfigureServices(IServiceCollection services, StatelessServiceContext serviceContext)
        {
            services.AddLogging(logging =>
            {
                logging.ClearProviders(); // Remove default providers
                logging.AddApplicationInsights(telemetryConfiguration =>
                {
                    telemetryConfiguration.DefaultTelemetrySink.TelemetryProcessorChainBuilder.Use((next) => new ExceptionOnlyTelemetryProcessor(next)).Build();
                    telemetryConfiguration.ConnectionString = Environment.GetEnvironmentVariable(KeyCollection.InstrumentationConnectionString)!;
                }, loggerOptions =>
                {
                    loggerOptions.TrackExceptionsAsExceptionTelemetry = true;
                    loggerOptions.FlushOnDispose = true;
                }); // Log to Application Insights
            });

            services.Configure<CookiePolicyOptions>(options =>
            {
                options.CheckConsentNeeded = context => true;
                options.MinimumSameSitePolicy = SameSiteMode.None;
            });

            ServiceImplementation.ConfigureServices(services, serviceContext);

            services.AddIdentity<AppIdentityUser, AppIdentityRole>()
                    .AddEntityFrameworkStores<AppIdentityDbContext>()
                    .AddDefaultTokenProviders();

            var tokenKey = Environment.GetEnvironmentVariable("Secret")!;
            var key = Encoding.ASCII.GetBytes(tokenKey);

            services.AddAuthentication(x =>
            {
                x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            }).AddJwtBearer(x =>
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
        }
    }

    public class ExceptionOnlyTelemetryProcessor : ITelemetryProcessor
    {
        private ITelemetryProcessor _next;

        public ExceptionOnlyTelemetryProcessor(ITelemetryProcessor next)
        {
            _next = next;
        }

        public void Process(ITelemetry item)
        {
            if (item is ExceptionTelemetry temeletryException)
            {
                // Allow exception telemetry through
                _next.Process(temeletryException);
            }
            // Otherwise, ignore the telemetry item (not send it)
        }
    }
}
