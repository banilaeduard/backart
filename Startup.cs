using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Diagnostics;

using Microsoft.AspNetCore.Authentication.JwtBearer;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Text.Json.Serialization;

using Microsoft.Extensions.Hosting;

using WebApi.Helpers;
using WebApi.Services;
using DataAccess.Entities;
using DataAccess.Context;
using CronJob;
using DataAccess;
using BackArt.Services;
using core;
using Storage;

namespace BackArt
{
    public delegate IBaseContextAccesor ServiceResolver(string serviceType);
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddHealthChecks();
            services.AddCors();
            services.AddControllers().AddJsonOptions(opt =>
            {
                opt.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
                opt.JsonSerializerOptions.IgnoreNullValues = true;
                opt.JsonSerializerOptions.NumberHandling = JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString;
                opt.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
            }).AddNewtonsoftJson(x => x.SerializerSettings.ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore);

            services.Configure<CookiePolicyOptions>(options =>
            {
                options.CheckConsentNeeded = context => true;
                options.MinimumSameSitePolicy = SameSiteMode.None;
            });

            // configure strongly typed settings objects
            var appSettingsSection = Configuration.GetSection("AppSettings");
            services.Configure<AppSettings>(appSettingsSection);
            services.AddSingleton(appSettingsSection.Get<AppSettings>());

            services.configureCoreProject();
            services.configureCronJob();
            services.configureDataAccess(Configuration);
            services.configureStorage();

            services.AddSingleton<EmailSender>();
            services.AddScoped<IUserService, UserService>();

            services.AddIdentity<AppIdentityUser, AppIdentityRole>()
                    .AddEntityFrameworkStores<AppIdentityDbContext>()
                    .AddDefaultTokenProviders();

            var tokenKey = appSettingsSection.GetValue<string>("Secret");
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
                    ValidateLifetime = true,
                };
                x.IncludeErrorDetails = true;
            });

            services.Configure<IdentityOptions>(opts =>
            {
                opts.User.RequireUniqueEmail = true;
                opts.Password.RequiredLength = 8;
                opts.SignIn.RequireConfirmedEmail = true;
            });
          
            services.AddHostedService<EmailReaderCronJob>();
            services.AddScoped<IBaseContextAccesor, HttpBaseContextAccesor>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, ILoggerFactory factory)
        {
            if (env.IsDevelopment())
            {
                 app.UseDeveloperExceptionPage();
            }
            else
            {
            ILogger genericLogger = factory.CreateLogger("Production unhandled logger");
            app.UseExceptionHandler(
                new ExceptionHandlerOptions()
                {
                    ExceptionHandler = async (context) =>
                    {
                        var feature = context.Features.Get<IExceptionHandlerFeature>();
                        genericLogger.LogError(
                            new EventId(context.TraceIdentifier.GetHashCode(), context.TraceIdentifier),
                            feature?.Error,
                            "Generic Error"
                            );

                        context.Response.ContentType = "application/json";
                        await context.Response.WriteAsJsonAsync(new
                        {
                            status = 500,
                            message = "Internal server error"
                        });
                    }
                });
            }

            app.UseCors(x => x
            .SetIsOriginAllowed(origin => true)
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials());

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                app.UseEndpoints(x => x.MapControllers());
                endpoints.MapHealthChecks("/health");
            });


        }
    }
}