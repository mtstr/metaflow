using System;
using System.Net.Http;
using EventStore.Client;
using Microsoft.Extensions.DependencyInjection;
using Orleans;
using Orleans.Hosting;

namespace Metaflow.Orleans
{
    public static class ConfigExtensions
    {
        public static IClientBuilder ConfigureMetaflow(this IClientBuilder builder,
            Action<WorkflowsClientBuilder> featuresBuilder)
        {
            builder.ConfigureServices((_, _) =>
            {
                var features = new WorkflowsClientBuilder();
                featuresBuilder(features);

                builder
                    .ConfigureApplicationParts(parts =>
                    {
                        parts.AddApplicationPart(typeof(FeatureGrain<,>).Assembly);
                        parts.AddApplicationPart(typeof(IFeatureGrain<,>).Assembly);
                        foreach (var assembly in features.Assemblies) parts.AddApplicationPart(assembly);
                    });
            });

            return builder;
        }

        public static ISiloBuilder ConfigureMetaflow(this ISiloBuilder builder, EventStoreConfig config,
            Action<WorkflowsBuilder> featuresBuilder)
        {
            var workflowsBuilder = new WorkflowsBuilder();


            builder.ConfigureServices((_, services) =>
                {
                    workflowsBuilder.Services = services;
                    featuresBuilder(workflowsBuilder);

                    services.AddHealthChecks();

//                        services.AddScoped<Monitoring.ITelemetryClient, AppInsightsTelemetryClient>();

                    services.AddControllers();

                    services.AddHttpClient();

                    services.AddEventStoreClient(settings =>
                    {
                        settings.ConnectivitySettings.Address = new Uri(config.Endpoint);
                        settings.DefaultCredentials =
                            new UserCredentials(config.User, config.Password);
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
                    parts.AddApplicationPart(typeof(FeatureGrain<,>).Assembly);
                    parts.AddApplicationPart(typeof(IFeatureGrain<,>).Assembly);
                    foreach (var assembly in workflowsBuilder.Assemblies) parts.AddApplicationPart(assembly);
                });
            return builder;
        }
    }
}