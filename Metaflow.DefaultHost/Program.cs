using System;
using System.Threading;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Metaflow.DefaultHost
{
    public class Program
    {
        private static readonly AutoResetEvent Closing = new AutoResetEvent(false);

        private static void OnExit(object sender, ConsoleCancelEventArgs args)
        {
            Closing.Set();
        }

        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args)
        {
            var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Local";

            var b = Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                })
                .ConfigureAppConfiguration((_, config) => config
                .AddEnvironmentVariables()
                .AddUserSecrets<Startup>());
                
            b.AddMetaflow();

            return b;
        }
    }
}
