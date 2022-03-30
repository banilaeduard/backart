using System;
using System.Threading.Tasks;
using System.Threading;
using Microsoft.Extensions.Hosting;
using DataAccess.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using DataAccess.Context;
using Microsoft.EntityFrameworkCore;

namespace DataAccess
{
    public class Initializer
    {
        private IServiceProvider providerScope;

        public Initializer(IServiceProvider provider)
        {
            providerScope = provider;
        }
        public async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                using (var provider = providerScope.CreateScope())
                {
                    // we ensure databases are created and up to date
                    var appIdentityDbContex = provider.ServiceProvider.GetRequiredService<AppIdentityDbContext>();
                    appIdentityDbContex.Database.Migrate();

                    var codeDbContex = provider.ServiceProvider.GetRequiredService<CodeDbContext>();
                    codeDbContex.Database.Migrate();

                    var complaintSeriesDbContext = provider.ServiceProvider.GetRequiredService<ComplaintSeriesDbContext>();
                    complaintSeriesDbContext.Database.Migrate();

                    var filterDbContext = provider.ServiceProvider.GetRequiredService<FilterDbContext>();
                    filterDbContext.Database.Migrate();

                    // we ensure roles are created
                    var roleManager = provider.ServiceProvider.GetRequiredService<RoleManager<AppIdentityRole>>();
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
                    var userManager = provider.ServiceProvider.GetRequiredService<UserManager<AppIdentityUser>>();
                    var user = await userManager.FindByNameAsync("admin");

                    if (user == null)
                    {
                        user = new AppIdentityUser()
                        {
                            UserName = "admin",
                            Email = "banila.eduard@gmail.com",
                            Tenant = "cubik",
                            DataKeyLocation = new DataKeyLocation() { name = "admin", locationCode = "admin" }
                        };
                        var result = await userManager.CreateAsync(user, "123EWQasd!@#");
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
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                await Task.Delay(5000).ContinueWith(async tsk => await this.ExecuteAsync(stoppingToken));
            }
        }
    }
}
