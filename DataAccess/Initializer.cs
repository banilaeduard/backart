using System;
using System.Threading.Tasks;
using System.Threading;
using System.Linq;
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
                    //if(!appIdentityDbContex.Database.EnsureCreated()) return;
                    //var appIden = appIdentityDbContex.Database.GenerateCreateScript();

                    var codeDbContex = provider.ServiceProvider.GetRequiredService<CodeDbContext>();
                    //var codeDb = codeDbContex.Database.GenerateCreateScript();

                    var complaintSeriesDbContext = provider.ServiceProvider.GetRequiredService<ComplaintSeriesDbContext>();
                    //var complaintdb = complaintSeriesDbContext.Database.GenerateCreateScript();

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

                    codeDbContex = new CodeDbContext(provider.ServiceProvider.GetRequiredService<DbContextOptions<CodeDbContext>>(),
                                                            provider.ServiceProvider.GetRequiredService<NoFilterBaseContext>());

                    var codeLinks = new string[] { "Pallas", "Allegro", "Vanity" };
                    var dbCodeLinks = codeDbContex.Codes.Where(t => codeLinks.Contains(t.CodeDisplay)).Select(t => t.CodeDisplay);
                    var except2 = codeLinks.Except(dbCodeLinks);

                    foreach (var article in except2)
                    {
                        codeDbContex.Codes.Add(new CodeLink()
                        {
                            CodeDisplay = article,
                            CodeValue = article,
                            TenantId = "cubik",
                            CreatedDate = DateTime.Now,
                            UpdatedDate = DateTime.Now,
                            Children = new CodeLink[4]
                            {
                                new CodeLink()
                                {
                                    CodeDisplay = "Pat",
                                    CodeValue = "Pat",
                                    isRoot = false,
                                    TenantId = "cubik",
                                    CreatedDate = DateTime.Now,
                                    UpdatedDate = DateTime.Now,
                                },
                                new CodeLink()
                                {
                                    CodeDisplay = "Noptiera",
                                    CodeValue = "Noptiera",
                                    isRoot = false,
                                    TenantId = "cubik",
                                    CreatedDate = DateTime.Now,
                                    UpdatedDate = DateTime.Now,
                                },
                                new CodeLink()
                                {
                                    CodeDisplay = "Dulap",
                                    CodeValue = "Dulap",
                                    isRoot = false,
                                    TenantId = "cubik",
                                    CreatedDate = DateTime.Now,
                                    UpdatedDate = DateTime.Now,
                                },
                                new CodeLink()
                                {
                                    CodeDisplay = "Comoda",
                                    CodeValue = "Comoda",
                                    isRoot = false,
                                    TenantId = "cubik",
                                    CreatedDate = DateTime.Now,
                                    UpdatedDate = DateTime.Now,
                                }
                            }.ToList(),
                            isRoot = true
                        });
                    }

                    if (except2?.Count() > 0)
                    {
                        await codeDbContex.SaveChangesAsync();
                    }

                    var codeAttributes = new string[] { "width", "length", "height" };
                    var dbCodeAttributes = codeDbContex.CodeAttribute.Where(t => codeAttributes.Contains(t.Tag)).Select(t => t.Tag);
                    var except3 = codeAttributes.Except(dbCodeAttributes);

                    foreach (var attribute in except3)
                    {
                        switch (attribute)
                        {
                            case "width":
                                codeDbContex.CodeAttribute.AddRange(new CodeAttribute()
                                {
                                    DisplayValue = attribute,
                                    Tag = attribute,
                                    InnerValue = "140",
                                    TenantId = "cubik"
                                },
                                new CodeAttribute()
                                {
                                    DisplayValue = attribute,
                                    Tag = attribute,
                                    InnerValue = "160",
                                    TenantId = "cubik"
                                },
                                new CodeAttribute()
                                {
                                    DisplayValue = attribute,
                                    Tag = attribute,
                                    InnerValue = "180",
                                    TenantId = "cubik"
                                }
                                ); break;
                            case "length":
                                codeDbContex.CodeAttribute.AddRange(new CodeAttribute()
                                {
                                    DisplayValue = attribute,
                                    Tag = attribute,
                                    InnerValue = "190",
                                    TenantId = "cubik"
                                },
                                new CodeAttribute()
                                {
                                    DisplayValue = attribute,
                                    Tag = attribute,
                                    InnerValue = "220",
                                    TenantId = "cubik"
                                },
                                new CodeAttribute()
                                {
                                    DisplayValue = attribute,
                                    Tag = attribute,
                                    InnerValue = "250",
                                    TenantId = "cubik"
                                }
                                ); break;
                            case "height":
                                codeDbContex.CodeAttribute.AddRange(new CodeAttribute()
                                {
                                    DisplayValue = attribute,
                                    Tag = attribute,
                                    InnerValue = "190",
                                    TenantId = "cubik"
                                },
                                new CodeAttribute()
                                {
                                    DisplayValue = attribute,
                                    Tag = attribute,
                                    InnerValue = "210",
                                    TenantId = "cubik"
                                },
                                new CodeAttribute()
                                {
                                    DisplayValue = attribute,
                                    Tag = attribute,
                                    InnerValue = "220",
                                    TenantId = "cubik"
                                }
                                ); break;
                        }
                    }

                    if (except3?.Count() > 0)
                    {
                        await codeDbContex.SaveChangesAsync();
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
