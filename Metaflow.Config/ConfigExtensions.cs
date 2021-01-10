using System;
using System.Net;
using System.Net.Http;
using EventStore.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.FSharp.Core;
using Orleans;
using Orleans.Configuration;
using Orleans.Hosting;

namespace Metaflow
{
    public static class ConfigExtensions
    {
        public static FeatureHandler<Delete, TModel, Unit> Then<TModel, TH>(this FeatureHandler<Delete, TModel, Unit> h)
            where TH : IStepHandler<TModel>
        {
            return FeatureHelper.andf<Delete, TModel, Unit, TH>(h);
        }

        public static FeatureHandler<T1, T2, T3> Then<T1, T2, T3, TH>(this FeatureHandler<T1, T2, T3> h)
            where TH : IStepHandler<T2>
        {
            return FeatureHelper.andf<T1, T2, T3, TH>(h);
        }

        public static FeatureHandler<T1, T2, T3> ThenInBackground<T1, T2, T3, TH>(this FeatureHandler<T1, T2, T3> h)
            where TH : IStepHandler<T2>
        {
            return FeatureHelper.andb<T1, T2, T3, TH>(h);
        }

        public static IServiceCollection AddMetaflowClient(this IServiceCollection services,
            MetaflowClientConfig config
            , Action<WorkflowsClientBuilder> featuresConfig
        )
        {
            var features = new WorkflowsClientBuilder();
            featuresConfig(features);
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

                    foreach (var assembly in features.Assemblies)
                    {
                        opt.SetServiceAssembly(assembly);
                    }

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
                            foreach (var assembly in features.Assemblies)
                            {
                                parts.AddApplicationPart(assembly).WithCodeGeneration();
                            }
                        });
                    };
                });
            });
            return services;
        }

        public static IHostBuilder AddMetaflowClient(this IHostBuilder builder,
            Action<WorkflowsClientBuilder> featuresBuilder)
        {
            builder.ConfigureServices((ctx, services) =>
            {
                var features = new WorkflowsClientBuilder();
                featuresBuilder(features);
                
                var config = ctx.Configuration.GetSection("Metaflow").Get<MetaflowClientConfig>();
                var clientBuilder = new ClientBuilder()
                    .Configure<ClusterOptions>(opts =>
                    {
                        opts.ClusterId = $"{config.ClusterName}Cluster";
                        opts.ServiceId = $"{config.ClusterName}Service";
                    })
                    .ConfigureApplicationParts(parts =>
                    {
                        parts.AddApplicationPart(typeof(IFeatureGrain<,,>).Assembly).WithCodeGeneration();
                        parts.AddApplicationPart(typeof(IConcurrencyScopeGrain).Assembly).WithCodeGeneration();
                        foreach (var assembly in features.Assemblies)
                        {
                            parts.AddApplicationPart(assembly);
                        }
                    });
                if (config.Local)
                {
                    clientBuilder.UseLocalhostClustering(config.GatewayPort);
                }
                else
                {
                    clientBuilder.UseRedisClustering(o => o.ConnectionString = config.OrleansStorage);
                }

                var client = clientBuilder.Build();
                client.Connect().Wait();
                services.AddSingleton<IClusterClient>(client);
                services.AddSingleton<FeatureClient>();
            });

            return builder;
        }

        public static IHostBuilder AddMetaflow(this IHostBuilder builder, Action<WorkflowsBuilder> featuresBuilder)
        {
            WorkflowsBuilder workflowsBuilder = new WorkflowsBuilder();


            builder.UseOrleans((ctx, siloBuilder) =>
            {
                var config = ctx.Configuration.GetSection("Metaflow").Get<MetaflowConfig>();

                siloBuilder
                    .Configure<ClusterOptions>(opts =>
                    {
                        opts.ClusterId = $"{config.ClusterName}Cluster";
                        opts.ServiceId = $"{config.ClusterName}Service";
                    })
                    .AddRedisGrainStorage("domainState", opt => opt.DataConnectionString = config.OrleansStorage)
                    .ConfigureServices(services =>
                    {
                        workflowsBuilder.Services = services;
                        featuresBuilder(workflowsBuilder);

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
                        featuresBuilder(workflowsBuilder);
                        parts.AddApplicationPart(typeof(IFeatureGrain<,,>).Assembly).WithCodeGeneration();

                        foreach (var assembly in workflowsBuilder.Assemblies)
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