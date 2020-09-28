using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.Loader;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Orleans;
using Orleans.Configuration;
using Orleans.Hosting;

namespace Metaflow.Orleans
{
    public static class HostBuilderExtensions
    {
        public static IHostBuilder AddMetaflow(this IHostBuilder builder, List<Assembly> assemblies = null)
        {
            builder.UseOrleans((Microsoft.Extensions.Hosting.HostBuilderContext ctx, ISiloBuilder siloBuilder) =>
                            {
                                var config = ctx.Configuration.GetSection("Metaflow").Get<MetaflowConfig>();

                                var metaflowAssemblies = assemblies ?? config.Assemblies.Where(a => !string.IsNullOrEmpty(a)).Select(a => AssemblyLoadContext.Default.LoadFromAssemblyPath(Path.GetFullPath(a))).ToList();


                                siloBuilder
                                .Configure<ClusterOptions>(opts =>
                                {
                                    opts.ClusterId = $"{config.ClusterName}Cluster";
                                    opts.ServiceId = $"{config.ClusterName}Service";
                                })
                                .AddCustomStorageBasedLogConsistencyProviderAsDefault()
                                .ConfigureApplicationParts(parts =>
                                {
                                    parts.AddApplicationPart(typeof(IRestfulGrain<>).Assembly).WithCodeGeneration();

                                    foreach (var assembly in metaflowAssemblies)
                                    {
                                        parts.AddApplicationPart(assembly).WithCodeGeneration();
                                    }
                                })
                                .ConfigureServices(services =>
                                {
                                    services.AddScoped(typeof(IDispatcher<>), typeof(ReflectionDispatcher<>));
                                    services.AddSingleton<ICustomEventStore, CustomEventStore>();
                                    services.AddSingleton<IEventRepository, CosmosEventRepository>();

                                    services.AddApplicationInsightsTelemetry();

                                    services.AddHealthChecks();

                                    services.AddScoped<ITelemetryClient, AppInsightsTelemetryClient>();

                                    services.AddControllers()
                                            .AddMetaflow(metaflowAssemblies);

                                    services.Scan(s => s.FromAssemblies(metaflowAssemblies).AddClasses(c => c.AssignableTo(typeof(IQuerySync<>))).AsImplementedInterfaces().WithScopedLifetime());

                                    services.Scan(s => s.FromAssemblies(metaflowAssemblies).AddClasses(c => c.AssignableTo(typeof(IQueryStore<>))).AsImplementedInterfaces().WithScopedLifetime());

                                    services.AddHttpClient();

                                    services.AddSingleton(p =>
                                    {
                                        return new CosmosClient(config.CosmosDb.Endpoint, config.CosmosDb.Key);
                                    });
                                    services.AddSingleton(p =>
                                    {
                                        var client = p.GetRequiredService<CosmosClient>();

                                        var streamContainer = client.GetContainer(config.CosmosDb.Database, config.CosmosDb.EventStreamContainer);

                                        var snapshotContainer = client.GetContainer(config.CosmosDb.Database, config.CosmosDb.SnapshotsContainer);

                                        return new CosmosEventContainers(snapshotContainer, streamContainer);
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
                                        .UseAzureStorageClustering(opt => opt.ConnectionString = config.AzureStorage)
                                        .ConfigureEndpoints(siloPort: config.SiloPort, gatewayPort: config.GatewayPort);
                                }
                            });


            return builder;
        }
    }
}