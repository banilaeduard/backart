using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

using WebApi.Entities;
using System.IO;
using System;

namespace BackArt
{
    public class Program
    {
        public async static Task Main(string[] args)
        {
            var host = CreateHostBuilder(args).Build();

            await Initialize(host.Services.GetRequiredService<IServiceScopeFactory>().CreateScope().ServiceProvider);
            DirectoryInfo di = new DirectoryInfo("/photos");
            Console.WriteLine("Exista? plm");
            Console.WriteLine(di.Exists);
            host.Run();
        }


        public static async Task Initialize(System.IServiceProvider serviceProvider)
        {
            // we ensure databases are created and up to date
            var appIdentityDbContex = serviceProvider.GetRequiredService<AppIdentityDbContext>();
            appIdentityDbContex.Database.Migrate();

            var codeDbContex = serviceProvider.GetRequiredService<CodeDbContext>();
            codeDbContex.Database.Migrate();

            var complaintSeriesDbContext = serviceProvider.GetRequiredService<ComplaintSeriesDbContext>();
            complaintSeriesDbContext.Database.Migrate();

            // we ensure roles are created
            var roleManager = serviceProvider.GetRequiredService<RoleManager<AppIdentityRole>>();
            AppIdentityRole role = null;

            if (!await roleManager.RoleExistsAsync("admin"))
            {
                role = new AppIdentityRole()
                {
                    Name = "admin"
                };
                await roleManager.CreateAsync(role);
                await roleManager.AddClaimAsync(role, new System.Security.Claims.Claim("star", "***"));
            }

            if (!await roleManager.RoleExistsAsync("partener"))
            {
                role = new AppIdentityRole()
                {
                    Name = "partener"
                };
                await roleManager.CreateAsync(role);
                await roleManager.AddClaimAsync(role, new System.Security.Claims.Claim("partenerClaim", "***"));
            }

            if (!await roleManager.RoleExistsAsync("basic"))
            {
                role = new AppIdentityRole()
                {
                    Name = "basic"
                };
                await roleManager.CreateAsync(role);
                await roleManager.AddClaimAsync(role, new System.Security.Claims.Claim("basicClaim", "***"));
            }

            // we ensure admin is created
            var userManager = serviceProvider.GetRequiredService<UserManager<AppIdentityUser>>();
            var user = await userManager.FindByNameAsync("admin");

            if (user == null)
            {
                user = new AppIdentityUser()
                {
                    UserName = "admin",
                    Email = "banila.eduard@gmail.com",
                };
                var result = await userManager.CreateAsync(user);
                if (result.Succeeded)
                {
                    await userManager.ConfirmEmailAsync(user, await userManager.GenerateEmailConfirmationTokenAsync(user));
                    await userManager.AddToRoleAsync(user, "admin");
                }
                else
                {
                    throw new System.Exception("admin nu a putut fi creat " + result.Errors.ToString());
                }
            }
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
            .ConfigureLogging(logBuilder =>
            {
                logBuilder.ClearProviders();
                logBuilder.AddConsole();
            })
            .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>().UseKestrel(options =>
                    {
                        options.Limits.MaxRequestBodySize = long.MaxValue;
                    });
                });
    }
}
