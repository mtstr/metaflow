using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Orleans;
using Orleans.Configuration;
using Orleans.Clustering.AzureStorage;
using Orleans.Hosting;
using Microsoft.Extensions.Configuration;
using Orleans.EventSourcing.Snapshot.Hosting;

namespace Metaflow.Orleans
{
    public static class StartupExtensions
    {
        public static IHostBuilder AddOrleans(this IHostBuilder builder, string clusterName)
        {
            builder.UseOrleans((Microsoft.Extensions.Hosting.HostBuilderContext ctx, ISiloBuilder siloBuilder) =>
                            {
                                var azureStorageConnection = ctx.Configuration.GetValue<string>("AzureTable");

                                siloBuilder
                                .UseLocalhostClustering()
                                .UseAzureStorageClustering(opt => opt.ConnectionString = azureStorageConnection)
                                .Configure<ClusterOptions>(opts =>
                                {
                                    opts.ClusterId = $"{clusterName}cluster";
                                    opts.ServiceId = $"{clusterName}service";
                                })
                                .AddLogStorageBasedLogConsistencyProviderAsDefault()
                                .ConfigureApplicationParts(parts =>
                                {
                                    parts.AddApplicationPart(typeof(IRestfulGrain<>).Assembly);
                                })
                                .ConfigureEndpoints(siloPort: 11111, gatewayPort: 30000)
                                // .Configure<EndpointOptions>(opts =>
                                // {
                                //     opts.AdvertisedIPAddress = IPAddress.Loopback;
                                // })
                                .ConfigureServices(ctx =>
                                {
                                    ctx.AddScoped(typeof(IDispatcher<>), typeof(ReflectionDispatcher<>));
                                })
                                .AddAzureTableGrainStorageAsDefault(ob =>
                                    ob.Configure<Microsoft.Extensions.Hosting.HostBuilderContext>((o, ctx) =>
                                    {
                                        o.UseJson = true;
                                        o.ConnectionString = azureStorageConnection;
                                    }))
                                .AddSnapshotStorageBasedConsistencyProviderAsDefault((op, name) =>
                                    {
                                        // Take snapshot every five events
                                        op.SnapshotStrategy = strategyInfo => strategyInfo.CurrentConfirmedVersion - strategyInfo.SnapshotVersion >= 5;
                                        op.UseIndependentEventStorage = false;
                                    })
                                    ;
                            });

            return builder;
        }

        public static IMvcBuilder AddMetaflow(this IMvcBuilder builder,
            ICollection<Type> resourceTypes)
        {
            builder.ConfigureApplicationPartManager(apm =>
                    {
                        apm.FeatureProviders.Add(new MutationControllerFeatureProvider(typeof(MutationController<,,>), resourceTypes));
                        apm.FeatureProviders.Add(new ReadControllerFeatureProvider(typeof(ReadController<>), resourceTypes));
                    });

            return builder;
        }

        public static IMvcBuilder AddMetaflow(this IMvcBuilder builder, Assembly assembly)
        {
            var types = assembly.GetExportedTypes();

            var resourceTypes = types.Where(t => t.GetCustomAttributes().Any(a => a is RestfulAttribute)).ToList();

            return builder.AddMetaflow(resourceTypes);
        }
    }
}
