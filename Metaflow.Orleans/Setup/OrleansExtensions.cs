using System.Collections.Generic;
using System.Net;
using System.Reflection;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Orleans;
using Orleans.Configuration;
using Orleans.Hosting;

namespace Metaflow.Orleans
{
    public static class OrleansExtensions
    {
        public static IHostBuilder AddOrleans(this IHostBuilder builder, IEnumerable<Assembly> assemblies)
        {
            builder.UseOrleans((Microsoft.Extensions.Hosting.HostBuilderContext ctx, ISiloBuilder siloBuilder) =>
                            {
                                var config = ctx.Configuration.GetSection("Metaflow").Get<OrleansConfig>();

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

                                    foreach (var assembly in assemblies)
                                    {
                                        parts.AddApplicationPart(assembly).WithCodeGeneration();
                                    }
                                })
                                .ConfigureServices(services =>
                                {
                                    services.AddScoped(typeof(IDispatcher<>), typeof(ReflectionDispatcher<>));
                                    services.AddSingleton<ICustomEventStore, CustomEventStore>();
                                    services.AddSingleton<IEventRepository, CosmosEventRepository>();

                                    services.AddSingleton(p =>
                                    {
                                        var client = new CosmosClient(config.CosmosDb.Endpoint, config.CosmosDb.Key);

                                        var streamContainer = client.GetContainer(config.CosmosDb.Database, config.CosmosDb.EventStreamContainer);

                                        var snapshotContainer = client.GetContainer(config.CosmosDb.Database, config.CosmosDb.SnapshotsContainer);

                                        return new CosmosEventContainers(snapshotContainer, streamContainer);
                                    });
                                });

                                if (config.Local)
                                {
                                    siloBuilder
                                        .UseLocalhostClustering()
                                        .Configure<EndpointOptions>(o => o.AdvertisedIPAddress = IPAddress.Loopback);
                                }
                                else
                                {
                                    siloBuilder
                                        .UseAzureStorageClustering(opt => opt.ConnectionString = config.AzureStorage)
                                        .ConfigureEndpoints(siloPort: 11111, gatewayPort: 30000);
                                }
                            });


            return builder;
        }
    }
}