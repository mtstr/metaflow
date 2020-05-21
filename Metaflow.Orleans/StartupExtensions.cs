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
