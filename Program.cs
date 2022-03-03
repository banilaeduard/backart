using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Builder;

using System.IO;
using System;

namespace BackArt
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var host = CreateHostBuilder(args).Build();

            DirectoryInfo di = new DirectoryInfo("/photos");
            Console.WriteLine("Exista? calea catre infinit");
            Console.WriteLine(di.Exists);
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
