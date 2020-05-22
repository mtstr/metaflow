using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Controllers;
using Orleans;

namespace Metaflow.Orleans
{
    public class MutationControllerFeatureProvider : IApplicationFeatureProvider<ControllerFeature>
    {
        private readonly Type _genericControllerType;
        private readonly IEnumerable<Type> _entityTypes;

        public MutationControllerFeatureProvider(Type genericControllerType, IEnumerable<Type> grainTypes)
        {
            _genericControllerType = genericControllerType;
            _entityTypes = grainTypes;
        }

        public void PopulateFeature(IEnumerable<ApplicationPart> parts, ControllerFeature feature)
        {
            foreach (var entityType in _entityTypes)
            {
                var name = entityType.Name;

                List<(Type, Type)> requestTypes = GetRequestForEntity(entityType);

                foreach (var requestType in requestTypes)
                {

                    TypeInfo controllerType = BuildClosedGenericControllerType(_genericControllerType, entityType, requestType.Item1, requestType.Item2).GetTypeInfo();

                    // if same or derived controller already defined
                    if (feature.Controllers.Any(c => controllerType.IsAssignableFrom(c)))
                    {
                        continue;
                    }

                    feature.Controllers.Add(controllerType);
                }
            }
        }


        private List<(Type, Type)> GetRequestForEntity(Type entityType)
        {
            var methods = entityType.GetMethods().Where(mi => mi.IsPublic).ToList();

            List<(Type, Type)> result = new List<(Type, Type)>();
            Action a = () => { };

            foreach (var m in methods)
            {
                var p = m.GetParameters().ToList();
                var t = m.Name.ToUpperInvariant() switch
                {
                    "PUT" when p.Count == 1 => AsPut(p),
                    "POST" when p.Count == 1 => AsPost(p),
                    _ => (null, null)
                };

                if (t != (null, null)) result.Add(t);
            }

            return result;
        }

        private static (Type, Type ParameterType) AsPost(List<ParameterInfo> parameters)
        {
            return (typeof(HttpPostAttribute), parameters[0].ParameterType);
        }

        private static (Type, Type) AsDelete(MethodInfo m)
        {
            return (typeof(HttpDeleteAttribute), m.GetGenericArguments().First());
        }

        private static (Type, Type ParameterType) AsPut(List<ParameterInfo> parameters)
        {
            return (typeof(HttpPutAttribute), parameters[0].ParameterType);
        }


        private static Type BuildClosedGenericControllerType(Type openControllerType, Type grainType, Type requestType, Type argType)
        {
            List<Type> genericTypeParams = new List<Type> { grainType, requestType, argType };

            Type genericControllerType = openControllerType.MakeGenericType(genericTypeParams.ToArray());

            return genericControllerType;
        }
    }
}
