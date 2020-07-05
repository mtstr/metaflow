using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace Metaflow.Orleans
{
    public static class StartupExtensions
    {
        public static IMvcBuilder AddMetaflow(this IMvcBuilder builder,
            ICollection<Type> resourceTypes)
        {
            builder.ConfigureApplicationPartManager(apm =>
                    {
                        apm.FeatureProviders.Add(new GrainControllerFeatureProvider(resourceTypes));
                    });

            builder.AddJsonOptions(o =>
            {
                o.JsonSerializerOptions.AddMaybeConverter();
            });

            return builder;
        }

        public static IMvcBuilder AddMetaflow(this IMvcBuilder builder, IEnumerable<Assembly> assemblies)
        {
            var types = assemblies.SelectMany(a => a.GetExportedTypes()).ToList();

            var resourceTypes = types.Where(t => t.GetCustomAttributes().Any(a => a is RestfulAttribute)).ToList();

            return builder.AddMetaflow(resourceTypes);
        }
    }
}
