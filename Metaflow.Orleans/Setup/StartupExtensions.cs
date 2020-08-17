using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json.Serialization;
using Microsoft.Extensions.DependencyInjection;

namespace Metaflow.Orleans
{
    public static class StartupExtensions
    {
        public static IMvcBuilder AddMetaflow(this IMvcBuilder builder,
            ICollection<Type> resourceTypes)
        {
            builder
            .AddJsonOptions(options =>{
                options.JsonSerializerOptions.IgnoreNullValues = true;
                options.JsonSerializerOptions.Converters.Add(new JsonFSharpConverter(unionTagCaseInsensitive: true, unionEncoding: JsonUnionEncoding.ExternalTag | JsonUnionEncoding.NamedFields | JsonUnionEncoding.UnwrapFieldlessTags | JsonUnionEncoding.UnwrapOption ));
            })
            .ConfigureApplicationPartManager(apm =>
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
