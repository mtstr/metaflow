using System;
using System.Threading;
using Metaflow.Tests.Client;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Metaflow.Tests.Host
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

            var b = Microsoft.Extensions.Hosting.Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                })
                .ConfigureAppConfiguration((_, config) => config
                .AddEnvironmentVariables()
                .AddUserSecrets<Startup>());
                
            b.AddMetaflow(cfg => { cfg.Delete<SampleModel>("sampleAggregate"); });

            return b;
        }
    }
}
