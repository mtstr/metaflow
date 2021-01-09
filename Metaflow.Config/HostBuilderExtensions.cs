using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
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
        public class WorkflowsClientConfig
        {
            private readonly IServiceCollection _services;

            public WorkflowsClientConfig(IServiceCollection services = null)
            {
                _services = services;
            }


            private WorkflowsClientConfig AddWorkflow(Feature f)
            {
                _services.AddSingleton(_ => f);

                return this;
            }

            public WorkflowsClientConfig Delete<TModel>(string aggregate, int version = 1) =>
                AddWorkflow(FeatureHelper.deleteValueFeature<TModel>(aggregate, version));
        }

        public class WorkflowsConfig
        {
            private readonly IServiceCollection _services;

            public WorkflowsConfig(IServiceCollection services = null)
            {
                _services = services;
            }

            public IReadOnlyCollection<Assembly> Assemblies => _assemblies.ToList().AsReadOnly();
            private readonly HashSet<Assembly> _assemblies = new();

            private WorkflowsConfig AddWorkflow<TOp, TModel, TInput>(FeatureHandler<TOp, TModel, TInput> h)
            {
                _services.AddSingleton(_ => h.Workflow);
                _services.AddSingleton(_ => h);
                _assemblies.Add(typeof(TModel).Assembly);
                _assemblies.Add(typeof(TInput).Assembly);

                return this;
            }

            public WorkflowsConfig Delete<TModel>(string aggregate, int version = 1) =>
                AddWorkflow(FeatureHelper.deleteValue<TModel>(aggregate, version).Then());
        }

        public static FeatureHandler<T1, T2, T3> Then<T1, T2, T3, TH>(this FeatureHandler<T1, T2, T3> h)
        {
            return FeatureHelper.andf<T1, T2, T3, TH>(h);
        }

        public static FeatureHandler<T1, T2, T3> ThenInBackground<T1, T2, T3, TH>(this FeatureHandler<T1, T2, T3> h)
        {
            return FeatureHelper.andb<T1, T2, T3, TH>(h);
        }

        public static IServiceCollection AddMetaflowClient(this IServiceCollection services,
            MetaflowClientConfig config, Action<WorkflowsClientConfig> featuresConfig)
        {
            featuresConfig(new WorkflowsClientConfig(services));
            services.AddSingleton<FeatureClient>();
            services.AddSingleton(ctx =>
                ctx.GetService<IOrleansClient>()?.GetClusterClient(typeof(IConcurrencyScopeGrain).Assembly));
            services.AddOrleansMultiClient(build =>
            {
                build.AddClient(opt =>
                {
                    opt.ClusterId = $"{config.ClusterName}Cluster";
                    opt.ServiceId = $"{config.ClusterName}Service";

                    opt.SetServiceAssembly(typeof(IConcurrencyScopeGrain).Assembly);

                    opt.Configure = b =>
                    {
                        if (config.Local)
                        {
                            b.UseLocalhostClustering(config.GatewayPort);
                        }
                        else
                        {
                            b.UseRedisClustering(o => o.ConnectionString = config.OrleansStorage);
                        }

                        b.ConfigureApplicationParts(parts =>
                        {
                            parts.AddApplicationPart(typeof(IFeatureGrain<,,>).Assembly).WithCodeGeneration();
                            parts.AddApplicationPart(typeof(IConcurrencyScopeGrain).Assembly).WithCodeGeneration();
                        });
                    };
                });
            });
            return services;
        }

        public static IHostBuilder AddMetaflow(this IHostBuilder builder, Action<WorkflowsConfig> featuresBuilder)
        {
            builder.UseOrleans((ctx, siloBuilder) =>
            {
                var config = ctx.Configuration.GetSection("Metaflow").Get<MetaflowConfig>();
                WorkflowsConfig workflowsConfig = new WorkflowsConfig();
                siloBuilder
                    .Configure<ClusterOptions>(opts =>
                    {
                        opts.ClusterId = $"{config.ClusterName}Cluster";
                        opts.ServiceId = $"{config.ClusterName}Service";
                    })
                    .AddRedisGrainStorage("domainState", opt => opt.DataConnectionString = config.OrleansStorage)
                    .ConfigureServices(services =>
                    {
                        workflowsConfig = new WorkflowsConfig(services);
                        featuresBuilder(workflowsConfig);

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
                    })
                    .ConfigureApplicationParts(parts =>
                    {
                        parts.AddApplicationPart(typeof(IFeatureGrain<,,>).Assembly).WithCodeGeneration();

                        foreach (var assembly in workflowsConfig.Assemblies)
                        {
                            parts.AddApplicationPart(assembly).WithCodeGeneration();
                        }
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