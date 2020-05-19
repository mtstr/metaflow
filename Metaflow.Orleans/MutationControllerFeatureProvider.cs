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

        private Dictionary<string, Type> RequestTypes = new Dictionary<string, Type>()
        {
            ["Put"] = typeof(HttpPutAttribute),
            ["Patch"] = typeof(HttpPutAttribute),
            ["Delete"] = typeof(HttpPutAttribute),
            ["Post"] = typeof(HttpPutAttribute)
        };

        private List<(Type, Type)> GetRequestForEntity(Type entityType)
        {
            var requests = entityType.GetMethods().Where(mi => mi.IsPublic && RequestTypes.Keys.Contains(mi.Name) && mi.GetParameters().ToList().Count == 1).Select(mi => ToRequest(mi)).ToList();

            return requests;
        }

        private (Type, Type) ToRequest(MethodInfo mi)
        {
            var args = mi.GetParameters().ToList();

            return (RequestTypes[mi.Name], args.First().ParameterType);
        }

        private static Type BuildClosedGenericControllerType(Type openControllerType, Type grainType, Type requestType, Type argType)
        {
            List<Type> genericTypeParams = new List<Type> { grainType, requestType, argType };

            Type genericControllerType = openControllerType.MakeGenericType(genericTypeParams.ToArray());

            return genericControllerType;
        }
    }
}
