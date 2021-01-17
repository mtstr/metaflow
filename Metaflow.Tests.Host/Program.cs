using System;
using System.Threading;
using Metaflow.Tests.Client;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

// [assembly: KnownAssembly(typeof(Metaflow.Tests.Client.SampleModel))]

namespace Metaflow.Tests.Host
{
    public class Program
    {
        private static readonly AutoResetEvent Closing = new(false);

        public static IHostBuilder CreateHostBuilder(string[] args)
        {
            var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Local";

            var b = Microsoft.Extensions.Hosting.Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder => { webBuilder.UseStartup<Startup>(); })
                .ConfigureAppConfiguration((_, config) => config
                    .AddEnvironmentVariables()
                    .AddUserSecrets<Startup>())
                .UseMetaflow(cfg =>
                {
                    cfg.Delete<SampleModel>("sampleAggregate", f: w => w.Then<SampleModel, FirstStepHandler>());
                });

            return b;
        }

        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        private static void OnExit(object sender, ConsoleCancelEventArgs args)
        {
            Closing.Set();
        }
    }
}