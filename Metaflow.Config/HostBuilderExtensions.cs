using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Runtime.Loader;
using EventStore.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Orleans;
using Orleans.Configuration;
using Orleans.Hosting;

namespace Metaflow
{
    public static class HostBuilderExtensions
    {
        public static IHostBuilder AddMetaflow(this IHostBuilder builder, List<Assembly> assemblies = null)
        {
            builder.UseOrleans((Microsoft.Extensions.Hosting.HostBuilderContext ctx, ISiloBuilder siloBuilder) =>
            {
                var config = ctx.Configuration.GetSection("Metaflow").Get<MetaflowConfig>();

                List<Assembly> metaflowAssemblies = assemblies ?? config.Assemblies.Where(a => !string.IsNullOrEmpty(a))
                    .Select(a => AssemblyLoadContext.Default.LoadFromAssemblyPath(Path.GetFullPath(a))).ToList();


                siloBuilder
                    .AddCustomStorageBasedLogConsistencyProviderAsDefault()
                    .Configure<ClusterOptions>(opts =>
                    {
                        opts.ClusterId = $"{config.ClusterName}Cluster";
                        opts.ServiceId = $"{config.ClusterName}Service";
                    })
                    .AddRedisGrainStorage("domainState")
                    .ConfigureApplicationParts(parts =>
                    {
                        parts.AddApplicationPart(typeof(IStateGrain<>).Assembly).WithCodeGeneration();
                        parts.AddApplicationPart(typeof(IFeatureGrain<,>).Assembly).WithCodeGeneration();
                        parts.AddApplicationPart(typeof(IAggregateGrain).Assembly).WithCodeGeneration();

                        foreach (var assembly in metaflowAssemblies)
                        {
                            parts.AddApplicationPart(assembly).WithCodeGeneration();
                        }
                    })
                    .ConfigureServices(services =>
                    {
                        services.AddApplicationInsightsTelemetry();

                        services.AddHealthChecks();

//                        services.AddScoped<Monitoring.ITelemetryClient, AppInsightsTelemetryClient>();

                        services.AddControllers();

                        services.AddHttpClient();

                        services.AddEventStoreClient(settings =>
                        {
                            settings.ConnectivitySettings.Address = new Uri(config.EventStore.Endpoint);
                            settings.DefaultCredentials =
                                new UserCredentials(config.EventStore.User, config.EventStore.Password);
                            settings.CreateHttpMessageHandler = () =>
                                new SocketsHttpHandler
                                {
                                    SslOptions =
                                    {
                                        RemoteCertificateValidationCallback = delegate { return true; }
                                    }
                                };
                        });
                    });

                if (config.Local)
                {
                    siloBuilder
                        .UseLocalhostClustering(siloPort: config.SiloPort, gatewayPort: config.GatewayPort)
                        .Configure<EndpointOptions>(o => o.AdvertisedIPAddress = IPAddress.Loopback);
                }
                else
                {
                    siloBuilder
                        .UseRedisClustering(opt => opt.ConnectionString = config.OrleansStorage)
                        .ConfigureEndpoints(siloPort: config.SiloPort, gatewayPort: config.GatewayPort);
                }
            });


            return builder;
        }
    }
}