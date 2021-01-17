using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

// [assembly: KnownAssembly(typeof(Metaflow.Tests.Client.SampleModel))]
namespace Metaflow.Tests.Client
{
    public class Program
    {
        public static IHostBuilder CreateHostBuilder(string[] args)
        {
            return Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder => { webBuilder.UseStartup<Startup>(); });
        }

        public static void Main(string[] args)
        {
            CreateHostBuilder(args).UseMetaflow(features => features.Add<SampleModel>()).Build()
                .Run();
        }
    }
}