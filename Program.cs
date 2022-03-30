using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Builder;

using Microsoft.Extensions.DependencyInjection;
using System;

namespace BackArt
{
    public class Program
    {
        public static bool IsInitializing = true;
        public static void Main(string[] args)
        {
            var host = CreateHostBuilder(args).Build();

            host.Services.GetRequiredService<IServiceScopeFactory>()
                .CreateScope().ServiceProvider
                .GetRequiredService<DataAccess.Initializer>()
                .ExecuteAsync(System.Threading.CancellationToken.None)
                .ContinueWith(t =>
                {
                    IsInitializing = false;
                    Console.WriteLine("SYSTEM INITIALIZED");
                });

            host.Run();
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
