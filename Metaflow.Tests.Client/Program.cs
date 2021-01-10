using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Orleans.CodeGeneration;

// [assembly: KnownAssembly(typeof(Metaflow.Tests.Client.SampleModel))]
namespace Metaflow.Tests.Client
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).AddMetaflowClient(features => features.Add<SampleModel>()).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder => { webBuilder.UseStartup<Startup>(); });
    }
}