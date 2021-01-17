using System;
using System.Net;
using Metaflow.Orleans;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Orleans.Configuration;
using Orleans.Hosting;

namespace Metaflow.Tests.Host
{
    public static class HostBuilderExtensions
    {
        public static IHostBuilder UseMetaflow(this IHostBuilder builder, Action<WorkflowsBuilder> workflowsBuilder)
        {
            builder.UseOrleans((ctx, siloBuilder) =>
            {
                var metaflowConfig = ctx.Configuration.GetSection("Metaflow").Get<MetaflowConfig>();
                var orleansConfig = metaflowConfig.Orleans;

                siloBuilder
                    .Configure<ClusterOptions>(opts =>
                    {
                        opts.ClusterId = $"{orleansConfig.ClusterName}Cluster";
                        opts.ServiceId = $"{orleansConfig.ClusterName}Service";
                    })
                    .AddRedisGrainStorageAsDefault(opt => opt.DataConnectionString = orleansConfig.Storage)
                    ;
                siloBuilder.ConfigureMetaflow(metaflowConfig.EventStore, workflowsBuilder);
                if (orleansConfig.Local)
                    siloBuilder
                        .UseLocalhostClustering(orleansConfig.SiloPort,
                            orleansConfig.GatewayPort)
                        .Configure<EndpointOptions>(o => o.AdvertisedIPAddress = IPAddress.Loopback);
                else
                    siloBuilder
                        .UseRedisClustering(opt => opt.ConnectionString = orleansConfig.Storage)
                        .ConfigureEndpoints(orleansConfig.SiloPort, orleansConfig.GatewayPort);
            });
            return builder;
        }
    }
}