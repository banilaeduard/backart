using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Diagnostics;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Text.Json.Serialization;

using WebApi.Helpers;
using WebApi.Services;
using WebApi.Entities;

namespace BackArt
{
    public class Startup
    {

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        private void configureConnectionString(IConfiguration Configuration, DbContextOptionsBuilder options)
        {
            var sqlOpt = Configuration["ConnectionStrings:instanceType"];
            switch (sqlOpt)
            {
                case "mysql": 
                    options.UseMySql(Configuration["ConnectionStrings:DefaultConnection"], ServerVersion.AutoDetect(Configuration["ConnectionStrings:DefaultConnection"]));
                    return;
                default:
                    options.UseSqlServer(Configuration["ConnectionStrings:DefaultConnection"]);
                    return;
            }
        }

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
            services.AddSingleton<AppSettings>(appSettingsSection.Get<AppSettings>());

            services.AddSingleton<EmailSender>();
            services.AddScoped<IUserService, UserService>();

            services.AddDbContext<ComplaintSeriesDbContext>(options =>
            {
                configureConnectionString(Configuration, options);
            }, ServiceLifetime.Transient);
            services.AddDbContext<CodeDbContext>(options => configureConnectionString(Configuration, options));
            services.AddDbContext<AppIdentityDbContext>(options => configureConnectionString(Configuration, options));
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
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, ILoggerFactory factory)
        {
            // if (env.IsDevelopment())
            // {
            //     app.UseDeveloperExceptionPage();
            // }
            // else
            // {
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
            //}

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