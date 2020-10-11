using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Runtime.Loader;
using EventStore.Client;
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

                List<Assembly> metaflowAssemblies = assemblies ?? config.Assemblies.Where(a => !string.IsNullOrEmpty(a))
                    .Select(a => AssemblyLoadContext.Default.LoadFromAssemblyPath(Path.GetFullPath(a))).ToList();


                siloBuilder
                    .AddCustomStorageBasedLogConsistencyProviderAsDefault()
                    .Configure<ClusterOptions>(opts =>
                    {
                        opts.ClusterId = $"{config.ClusterName}Cluster";
                        opts.ServiceId = $"{config.ClusterName}Service";
                    })
                    .AddStateStorageBasedLogConsistencyProviderAsDefault()
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

                        services.AddApplicationInsightsTelemetry();

                        services.AddHealthChecks();

                        services.AddScoped<ITelemetryClient, AppInsightsTelemetryClient>();

                        services.AddControllers()
                            .AddMetaflow(metaflowAssemblies);

                        services.Scan(s =>
                            s.FromAssemblies(metaflowAssemblies).AddClasses(c => c.AssignableTo(typeof(IQuerySync<>)))
                                .AsImplementedInterfaces().WithScopedLifetime());

                        services.Scan(s =>
                            s.FromAssemblies(metaflowAssemblies).AddClasses(c => c.AssignableTo(typeof(IQueryStore<>)))
                                .AsImplementedInterfaces().WithScopedLifetime());

                        services.AddHttpClient();

                        services.AddSingleton<IEventSerializer, EventSerializer>();
                        services.AddSingleton(_ =>
                        {
                            List<Type> types = metaflowAssemblies.SelectMany(a => a.GetExportedTypes()).ToList();
                            List<Type> resourceTypes = types.Where(t => t.GetCustomAttributes().Any(a => a is RestfulAttribute)).ToList();
                            return UpgradeMap.Initialize(resourceTypes);
                        });
                        services.AddEventStoreClient(settings =>
                        {
                            settings.ConnectivitySettings.Address = new Uri(config.EventStore.Endpoint);
                            settings.DefaultCredentials = new UserCredentials(config.EventStore.User, config.EventStore.Password);
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
                        .UseAzureStorageClustering(opt => opt.ConnectionString = config.AzureStorage)
                        .ConfigureEndpoints(siloPort: config.SiloPort, gatewayPort: config.GatewayPort);
                }
            });


            return builder;
        }
    }
}