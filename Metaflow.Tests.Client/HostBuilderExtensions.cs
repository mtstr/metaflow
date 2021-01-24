using System;
using Metaflow.Orleans;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Orleans;
using Orleans.Configuration;

namespace Metaflow.Tests.Client
{
    public static class HostBuilderExtensions
    {
        public static IHostBuilder UseMetaflow(this IHostBuilder builder,
            Action<WorkflowsClientBuilder> featuresBuilder)
        {
            builder.ConfigureServices((ctx, services) =>
            {
                var clientBuilder = new ClientBuilder();

                var config = ctx.Configuration.GetSection("Metaflow").Get<OrleansConfig>();

                clientBuilder
                    .Configure<ClusterOptions>(opts =>
                    {
                        opts.ClusterId = $"{config.ClusterName}Cluster";
                        opts.ServiceId = $"{config.ClusterName}Service";
                    });

                clientBuilder.ConfigureMetaflow(featuresBuilder);
                services.AddSingleton<FeatureClient>();

                if (config.Local)
                    clientBuilder.UseLocalhostClustering(config.GatewayPort);
                else
                    clientBuilder.UseRedisClustering(o => o.ConnectionString = config.Storage);

                var client = clientBuilder.Build();

                client.Connect().Wait();
                services.AddSingleton(client);
            });

            return builder;
        }
    }
}