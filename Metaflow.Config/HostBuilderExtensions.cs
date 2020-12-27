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
        public class FeaturesClientConfig
        {
            private readonly IServiceCollection _services;

            public FeaturesClientConfig(IServiceCollection services = null)
            {
                _services = services;
            }


            private FeaturesClientConfig AddFeature(Feature f)
            {
                _services.AddSingleton(_ => f);

                return this;
            }

            public FeaturesClientConfig Delete<TModel>(string aggregate, int version = 1) =>
                AddFeature(FeatureHelper.deleteValue<TModel>(aggregate, version).Feature);
        }

        public class FeaturesConfig
        {
            private readonly IServiceCollection _services;

            public FeaturesConfig(IServiceCollection services = null)
            {
                _services = services;
            }

            public IReadOnlyCollection<Assembly> Assemblies => _assemblies.ToList().AsReadOnly();
            private readonly HashSet<Assembly> _assemblies = new();

            private FeaturesConfig AddFeature<TOp, TModel, TInput>(FeatureHandler<TOp, TModel, TInput> h)
            {
                _services.AddSingleton(_ => h.Feature);
                _services.AddSingleton(_ => h);
                _assemblies.Add(typeof(TModel).Assembly);
                _assemblies.Add(typeof(TInput).Assembly);

                return this;
            }

            public FeaturesConfig Delete<TModel>(string aggregate, int version = 1) =>
                AddFeature(FeatureHelper.deleteValue<TModel>(aggregate, version));
        }


        public static IServiceCollection AddMetaflowClient(this IServiceCollection services,
            MetaflowClientConfig config, Action<FeaturesClientConfig> featuresConfig)
        {
            featuresConfig(new FeaturesClientConfig(services));
            services.AddSingleton<FeatureClient>();
            services.AddSingleton(ctx =>
                ctx.GetService<IOrleansClient>()?.GetClusterClient(typeof(IStateGrain<>).Assembly));
            services.AddOrleansMultiClient(build =>
            {
                build.AddClient(opt =>
                {
                    opt.ClusterId = $"{config.ClusterName}Cluster";
                    opt.ServiceId = $"{config.ClusterName}Service";

                    opt.SetServiceAssembly(typeof(IStateGrain<>).Assembly);

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
                            parts.AddApplicationPart(typeof(IStateGrain<>).Assembly).WithCodeGeneration();
                            parts.AddApplicationPart(typeof(IFeatureGrain<,,>).Assembly).WithCodeGeneration();
                            parts.AddApplicationPart(typeof(IConcurrencyScopeGrain).Assembly).WithCodeGeneration();
                        });
                    };
                });
            });
            return services;
        }

        public static IHostBuilder AddMetaflow(this IHostBuilder builder, Action<FeaturesConfig> featuresBuilder)
        {
            builder.UseOrleans((ctx, siloBuilder) =>
            {
                var config = ctx.Configuration.GetSection("Metaflow").Get<MetaflowConfig>();
                FeaturesConfig featuresConfig = new FeaturesConfig();
                siloBuilder
                    .AddCustomStorageBasedLogConsistencyProviderAsDefault()
                    .Configure<ClusterOptions>(opts =>
                    {
                        opts.ClusterId = $"{config.ClusterName}Cluster";
                        opts.ServiceId = $"{config.ClusterName}Service";
                    })
                    .AddRedisGrainStorage("domainState", opt => opt.DataConnectionString = config.OrleansStorage)
                    .ConfigureServices(services =>
                    {
                        featuresConfig = new FeaturesConfig(services);
                        featuresBuilder(featuresConfig);

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
                        parts.AddApplicationPart(typeof(IStateGrain<>).Assembly).WithCodeGeneration();
                        parts.AddApplicationPart(typeof(IFeatureGrain<,,>).Assembly).WithCodeGeneration();
                        parts.AddApplicationPart(typeof(IConcurrencyScopeGrain).Assembly).WithCodeGeneration();

                        foreach (var assembly in featuresConfig.Assemblies)
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