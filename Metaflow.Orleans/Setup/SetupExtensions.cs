using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.DependencyInjection;

namespace Metaflow.Orleans
{
    public static class SetupExtensions
    {
        public static JsonFSharpConverter FSharpJsonConverter = new JsonFSharpConverter(unionTagCaseInsensitive: true, unionEncoding: JsonUnionEncoding.ExternalTag | JsonUnionEncoding.NamedFields | JsonUnionEncoding.UnwrapFieldlessTags | JsonUnionEncoding.UnwrapOption);


        public static JsonSerializerOptions Configure(this JsonSerializerOptions options)
        {
            options.Converters.Add(FSharpJsonConverter);
            options.PropertyNameCaseInsensitive = true;
            options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            return options;
        }
        public static IMvcBuilder AddMetaflow(this IMvcBuilder builder,
            ICollection<Type> resourceTypes)
        {
            builder
            .AddJsonOptions(options => options.JsonSerializerOptions.Configure())
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
            List<Type> types = assemblies.SelectMany(a => a.GetExportedTypes()).ToList();

            List<Type> resourceTypes = types.Where(t => t.GetCustomAttributes().Any(a => a is RestfulAttribute)).ToList();

            return builder.AddMetaflow(resourceTypes);
        }
    }
}
